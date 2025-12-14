/**
 * Copyright (c) 2022 Raspberry Pi (Trading) Ltd.
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

//
// Created by elliot on 25/05/24.
//
#include "pico/stdlib.h"
#include "pico/cyw43_arch.h"
#include "pico/unique_id.h"
#include "hardware/gpio.h"
#include "hardware/irq.h"
#include "hardware/adc.h"
#include "hardware/dma.h"
#include "lwip/apps/mqtt.h"
#include "lwip/apps/mqtt_priv.h" // needed to set hostname
#include "lwip/dns.h"
#include "lwip/altcp_tls.h"

// Temperature
#ifndef TEMPERATURE_UNITS
#define TEMPERATURE_UNITS 'C' // Set to 'F' for Fahrenheit
#endif

#ifndef MQTT_SERVER
#error Need to define MQTT_SERVER
#endif

// This file includes your client certificate for client server authentication
#ifdef MQTT_CERT_INC
#include MQTT_CERT_INC
#endif

#ifndef MQTT_TOPIC_LEN
#define MQTT_TOPIC_LEN 100
#endif

typedef struct {
    mqtt_client_t* mqtt_client_inst;
    struct mqtt_connect_client_info_t mqtt_client_info;
    char data[MQTT_OUTPUT_RINGBUF_SIZE];
    char topic[MQTT_TOPIC_LEN];
    uint32_t len;
    ip_addr_t mqtt_server_address;
    bool connect_done;
    int subscribe_count;
    bool stop_client;
} MQTT_CLIENT_DATA_T;

#ifndef DEBUG_printf
#ifndef NDEBUG
#define DEBUG_printf printf
#else
#define DEBUG_printf(...)
#endif
#endif

#ifndef INFO_printf
#define INFO_printf printf
#endif

#ifndef ERROR_printf
#define ERROR_printf printf
#endif

// how often to measure our temperature
#define TEMP_WORKER_TIME_S 10

// keep alive in seconds
#define MQTT_KEEP_ALIVE_S 60

// qos passed to mqtt_subscribe
// At most once (QoS 0)
// At least once (QoS 1)
// Exactly once (QoS 2)
#define MQTT_SUBSCRIBE_QOS 1
#define MQTT_PUBLISH_QOS 1
#define MQTT_PUBLISH_RETAIN 0

// topic used for last will and testament
#define MQTT_WILL_TOPIC "/online"
#define MQTT_WILL_MSG "0"
#define MQTT_WILL_QOS 1

#ifndef MQTT_DEVICE_NAME
#define MQTT_DEVICE_NAME "pico"
#endif

// Set to 1 to add the client name to topics, to support multiple devices using the same server
#ifndef MQTT_UNIQUE_TOPIC
#define MQTT_UNIQUE_TOPIC 0
#endif

#define ADC_DMA_BUF_LEN 32
static uint16_t adc_dma_buf[ADC_DMA_BUF_LEN];
static volatile uint16_t adc_last_published = 0xFFFF;
static volatile bool adc_changed_flag = false;
static volatile uint16_t adc_new_value = 0;

#define ADC_MAX_VALUE 4095.0f // 12-bit ADC

static int adc_dma_chan;
static dma_channel_config adc_dma_cfg;
static MQTT_CLIENT_DATA_T *client_state;
// Minimal threshold to avoid noise flips (set to 0 to detect any change)
#define ADC_CHANGE_THRESHOLD 4
static char connected_table_id[MQTT_TOPIC_LEN] = {0};

// Call from main (not IRQ) to publish or handle the new value
// Example placeholder — replace with your mqtt_publish call

/* References for this implementation:
 * raspberry-pi-pico-c-sdk.pdf, Section '4.1.1. hardware_adc'
 * pico-examples/adc/adc_console/adc_console.c */
// static float read_onboard_temperature(const char unit) {

//     /* 12-bit conversion, assume max value == ADC_VREF == 3.3 V */
//     const float conversionFactor = 3.3f / (1 << 12);

//     float adc = (float)adc_read() * conversionFactor;
//     float tempC = 27.0f - (adc - 0.706f) / 0.001721f;

//     if (unit == 'C' || unit != 'F') {
//         return tempC;
//     } else if (unit == 'F') {
//         return tempC * 9 / 5 + 32;
//     }

//     return -1.0f;
// }

static void pub_request_cb(__unused void *arg, err_t err) {
    if (err != 0) {
        ERROR_printf("pub_request_cb failed %d", err);
    }
}

static const char *full_topic(MQTT_CLIENT_DATA_T *state, const char *name) {
#if MQTT_UNIQUE_TOPIC
    static char full_topic[MQTT_TOPIC_LEN];
    snprintf(full_topic, sizeof(full_topic), "/%s%s", state->mqtt_client_info.client_id, name);
    return full_topic;
#else
    return name;
#endif
}

static void control_led(MQTT_CLIENT_DATA_T *state, bool on) {
    // Publish state on /state topic and on/off led board
    const char* message = on ? "On" : "Off";
    if (on)
        cyw43_arch_gpio_put(CYW43_WL_GPIO_LED_PIN, 1);
    else
        cyw43_arch_gpio_put(CYW43_WL_GPIO_LED_PIN, 0);

    mqtt_publish(state->mqtt_client_inst, full_topic(state, "/led/state"), message, strlen(message), MQTT_PUBLISH_QOS, MQTT_PUBLISH_RETAIN, pub_request_cb, state);
}

// static void publish_temperature(MQTT_CLIENT_DATA_T *state) {
//     static float old_temperature;
//     const char *temperature_key = full_topic(state, "/temperature");
//     float temperature = read_onboard_temperature(TEMPERATURE_UNITS);
//     if (temperature != old_temperature) {
//         old_temperature = temperature;
//         // Publish temperature on /temperature topic
//         char temp_str[16];
//         snprintf(temp_str, sizeof(temp_str), "%.2f", temperature);
//         INFO_printf("Publishing %s to %s\n", temp_str, temperature_key);
//         mqtt_publish(state->mqtt_client_inst, temperature_key, temp_str, strlen(temp_str), MQTT_PUBLISH_QOS, MQTT_PUBLISH_RETAIN, pub_request_cb, state);
//     }
// }



static void sub_request_cb(void *arg, err_t err) {
    MQTT_CLIENT_DATA_T* state = (MQTT_CLIENT_DATA_T*)arg;
    if (err != 0) {
        panic("subscribe request failed %d", err);
    }
    state->subscribe_count++;
}

static void unsub_request_cb(void *arg, err_t err) {
    MQTT_CLIENT_DATA_T* state = (MQTT_CLIENT_DATA_T*)arg;
    if (err != 0) {
        panic("unsubscribe request failed %d", err);
    }
    state->subscribe_count--;
    assert(state->subscribe_count >= 0);

    // Stop if requested
    if (state->subscribe_count <= 0 && state->stop_client) {
        mqtt_disconnect(state->mqtt_client_inst);
    }
}

static void sub_unsub_topics(MQTT_CLIENT_DATA_T* state, bool sub) {
    char topic[MQTT_TOPIC_LEN];
    sprintf(topic, "/checkForTable/%s", state->mqtt_client_info.client_id);
    mqtt_request_cb_t cb = sub ? sub_request_cb : unsub_request_cb;

}
static void initialize_pico(MQTT_CLIENT_DATA_T* state) {
    char topic[MQTT_TOPIC_LEN];
    mqtt_publish(state->mqtt_client_inst, full_topic(state, MQTT_WILL_TOPIC), state->mqtt_client_info.client_id, sizeof(state->mqtt_client_info.client_id) - 1, MQTT_WILL_QOS, true, pub_request_cb, state);
    sprintf(topic, "/checkForTable/%s", state->mqtt_client_info.client_id);
    INFO_printf("Subscribing to topic %s\n", full_topic(state, topic));
    mqtt_request_cb_t cb = sub_request_cb;
    mqtt_sub_unsub(state->mqtt_client_inst, full_topic(state, topic), MQTT_SUBSCRIBE_QOS, cb, state, true);
}
static void initialize_dashboard(MQTT_CLIENT_DATA_T* state, u16_t len, const u8_t* data) {
    char topic[MQTT_TOPIC_LEN];
    snprintf(topic, sizeof(topic), "/tables/%s/height", data);
    INFO_printf("Subscribing to topic %s\n", full_topic(state, topic));
    snprintf(connected_table_id, sizeof(connected_table_id), "%s", (const char *)data);
    mqtt_subscribe(state->mqtt_client_inst, full_topic(state, topic), MQTT_SUBSCRIBE_QOS, sub_request_cb, state);
    snprintf(topic, sizeof(topic), "/checkForTable/%s", state->mqtt_client_info.client_id);
    mqtt_unsubscribe(state->mqtt_client_inst, full_topic(state, topic), unsub_request_cb, state);
    INFO_printf("Unsubscribing from topic %s\n", full_topic(state, topic));

}

static void mqtt_incoming_data_cb(void *arg, const u8_t *data, u16_t len, u8_t flags) {
    MQTT_CLIENT_DATA_T* state = (MQTT_CLIENT_DATA_T*)arg;
        char topic[MQTT_TOPIC_LEN];
    sprintf(topic, "/checkForTable/%s", state->mqtt_client_info.client_id);
#if MQTT_UNIQUE_TOPIC
    const char *basic_topic = state->topic + strlen(state->mqtt_client_info.client_id) + 1;
#else
    const char *basic_topic = state->topic;
#endif
    strncpy(state->data, (const char *)data, len);
    state->len = len;
    state->data[len] = '\0';

    INFO_printf("Topic: %s, Message: %s\n", state->topic, state->data);
    if (strcmp(basic_topic, topic) == 0) {
        INFO_printf("Received table check message: %.*s\nDashboard Init in progress\n", len, data);
        initialize_dashboard(state, len, data);

    } else if (strcmp(basic_topic, "/print") == 0) {
        INFO_printf("%.*s\n", len, data);
    } else if (strcmp(basic_topic, "/ping") == 0) {
        char buf[11];
        snprintf(buf, sizeof(buf), "%u", to_ms_since_boot(get_absolute_time()) / 1000);
        mqtt_publish(state->mqtt_client_inst, full_topic(state, "/uptime"), buf, strlen(buf), MQTT_PUBLISH_QOS, MQTT_PUBLISH_RETAIN, pub_request_cb, state);
    } else if (strcmp(basic_topic, "/exit") == 0) {
        state->stop_client = true; // stop the client when ALL subscriptions are stopped
        sub_unsub_topics(state, false); // unsubscribe
    }
}

static void mqtt_incoming_publish_cb(void *arg, const char *topic, u32_t tot_len) {
    MQTT_CLIENT_DATA_T* state = (MQTT_CLIENT_DATA_T*)arg;
    strncpy(state->topic, topic, sizeof(state->topic));
}

// static void temperature_worker_fn(async_context_t *context, async_at_time_worker_t *worker) {
//     MQTT_CLIENT_DATA_T* state = (MQTT_CLIENT_DATA_T*)worker->user_data;
//     publish_temperature(state);
//     async_context_add_at_time_worker_in_ms(context, worker, TEMP_WORKER_TIME_S * 1000);
// }
// static async_at_time_worker_t temperature_worker = { .do_work = temperature_worker_fn };

static void mqtt_connection_cb(mqtt_client_t *client, void *arg, mqtt_connection_status_t status) {
    MQTT_CLIENT_DATA_T* state = (MQTT_CLIENT_DATA_T*)arg;
    if (status == MQTT_CONNECT_ACCEPTED) {
        state->connect_done = true;
        initialize_pico(state); // subscribe;

        // indicate online
        if (state->mqtt_client_info.will_topic) {
            mqtt_publish(state->mqtt_client_inst, state->mqtt_client_info.will_topic, state->mqtt_client_info.client_id, sizeof(state->mqtt_client_info.client_id) - 1, MQTT_WILL_QOS, true, pub_request_cb, state);
        }

        // Publish temperature every 10 sec if it's changed
        // temperature_worker.user_data = state;
        // async_context_add_at_time_worker_in_ms(cyw43_arch_async_context(), &temperature_worker, 0);
    } else if (status == MQTT_CONNECT_DISCONNECTED) {
        if (!state->connect_done) {
            panic("Failed to connect to mqtt server");
        }
    }
    else {
        panic("Unexpected status");
    }
}

static void start_client(MQTT_CLIENT_DATA_T *state) {
#if LWIP_ALTCP && LWIP_ALTCP_TLS
    const int port = MQTT_TLS_PORT;
    INFO_printf("Using TLS\n");
#else
    const int port = MQTT_PORT;
    INFO_printf("Warning: Not using TLS\n");
#endif

    state->mqtt_client_inst = mqtt_client_new();
    if (!state->mqtt_client_inst) {
        panic("MQTT client instance creation error");
    }
    INFO_printf("IP address of this device %s\n", ipaddr_ntoa(&(netif_list->ip_addr)));
    INFO_printf("Connecting to mqtt server at %s\n", ipaddr_ntoa(&state->mqtt_server_address));

    cyw43_arch_lwip_begin();
    if (mqtt_client_connect(state->mqtt_client_inst, &state->mqtt_server_address, port, mqtt_connection_cb, state, &state->mqtt_client_info) != ERR_OK) {
        panic("MQTT broker connection error");
    }
#if LWIP_ALTCP && LWIP_ALTCP_TLS
    // This is important for MBEDTLS_SSL_SERVER_NAME_INDICATION
    mbedtls_ssl_set_hostname(altcp_tls_context(state->mqtt_client_inst->conn), MQTT_SERVER);
#endif
    mqtt_set_inpub_callback(state->mqtt_client_inst, mqtt_incoming_publish_cb, mqtt_incoming_data_cb, state);
    cyw43_arch_lwip_end();
}

// Call back with a DNS result
static void dns_found(const char *hostname, const ip_addr_t *ipaddr, void *arg) {
    MQTT_CLIENT_DATA_T *state = (MQTT_CLIENT_DATA_T*)arg;
    if (ipaddr) {
        state->mqtt_server_address = *ipaddr;
        start_client(state);
    } else {
        panic("dns request failed");
    }
}

static void get_adc_data_cb(uint gpio, uint32_t events) {
    // Read the latest ADC value from the DMA buffer
    uint16_t latest_value = adc_dma_buf[(ADC_DMA_BUF_LEN - 1)];
    char topic[MQTT_TOPIC_LEN];
    if (connected_table_id == NULL || connected_table_id[0] == '\0') {
        INFO_printf("No table connected, ignoring ADC value\n");
        return;
    }
    char payload[5];
 
    sprintf(topic, "/tables/%s/setHeight", connected_table_id);
    INFO_printf("/tables/%s/setHeight", connected_table_id);

    sprintf(payload, "%d", 680 + (int)((float)latest_value/ADC_MAX_VALUE * 660.0f)); // Map 0-4095 to 670-1320mm
    
    mqtt_publish(client_state->mqtt_client_inst, full_topic(client_state, topic), payload, sizeof(payload), MQTT_PUBLISH_QOS, MQTT_PUBLISH_RETAIN, pub_request_cb, client_state);
    INFO_printf("ADC Value: %u\n", latest_value);


}

static void adc_dma_irq_handler() {
    // clear IRQ for this channel
    dma_hw->ints0 = 1u << adc_dma_chan;

    // simply restart the DMA transfer so the buffer is refilled
    dma_channel_configure(adc_dma_chan, &adc_dma_cfg,
                          adc_dma_buf,            // dst
                          &adc_hw->fifo,          // src (ADC FIFO)
                          ADC_DMA_BUF_LEN,        // transfer count
                          true);                  // start immediately
}

static void adc_dma_init(void) {
    // Ensure ADC GPIO is prepared (GP26 -> ADC0). adc_select_input(0) is called in main,
    // but initializing the GPIO here is safe/redundant.
    adc_gpio_init(26);

    // Configure ADC FIFO: enable FIFO, enable IRQ (not used), DREQ threshold = 1, no error
    adc_fifo_setup(true, true, 1, false, false);

    // Start free-running conversions
    adc_run(true);

    // Claim a DMA channel
    adc_dma_chan = dma_claim_unused_channel(true);
    adc_dma_cfg = dma_channel_get_default_config(adc_dma_chan);

    // Configure DMA: 16-bit transfers, read from fixed ADC FIFO, write to incrementing RAM,
    // paced by ADC DREQ.
    channel_config_set_transfer_data_size(&adc_dma_cfg, DMA_SIZE_16);
    channel_config_set_read_increment(&adc_dma_cfg, false);
    channel_config_set_write_increment(&adc_dma_cfg, true);
    channel_config_set_dreq(&adc_dma_cfg, DREQ_ADC);

    // Configure and start the channel to continually fill adc_dma_buf
    dma_channel_configure(adc_dma_chan, &adc_dma_cfg,
                          adc_dma_buf,          // dst
                          &adc_hw->fifo,        // src
                          ADC_DMA_BUF_LEN,      // transfer count
                          true);                // start immediately

    // Enable IRQ for this channel and attach handler (handler restarts the transfer)
    dma_channel_set_irq0_enabled(adc_dma_chan, true);
    irq_set_exclusive_handler(DMA_IRQ_0, adc_dma_irq_handler);
    irq_set_enabled(DMA_IRQ_0, true);
}

int main(void) {
    stdio_init_all();
    INFO_printf("mqtt client starting\n");
    gpio_init(10);
    gpio_set_dir(10, GPIO_IN);
    gpio_pull_up(10);

    gpio_set_irq_enabled_with_callback(10, GPIO_IRQ_EDGE_FALL, true, get_adc_data_cb);

    adc_init();
    // adc_set_temp_sensor_enabled(true);
    adc_select_input(0);

    adc_dma_init();
    static MQTT_CLIENT_DATA_T state;

    if (cyw43_arch_init()) {
        panic("Failed to inizialize CYW43");
    }

    // Use board unique id
    char unique_id_buf[5];
    pico_get_unique_board_id_string(unique_id_buf, sizeof(unique_id_buf));
    for(int i=0; i < sizeof(unique_id_buf) - 1; i++) {
        unique_id_buf[i] = tolower(unique_id_buf[i]);
    }

    // Generate a unique name, e.g. pico1234
    char client_id_buf[sizeof(MQTT_DEVICE_NAME) + sizeof(unique_id_buf) - 1];
    memcpy(&client_id_buf[0], MQTT_DEVICE_NAME, sizeof(MQTT_DEVICE_NAME) - 1);
    memcpy(&client_id_buf[sizeof(MQTT_DEVICE_NAME) - 1], unique_id_buf, sizeof(unique_id_buf) - 1);
    client_id_buf[sizeof(client_id_buf) - 1] = 0;
    INFO_printf("Device name %s\n", client_id_buf);

    state.mqtt_client_info.client_id = client_id_buf;
    state.mqtt_client_info.keep_alive = MQTT_KEEP_ALIVE_S; // Keep alive in sec
#if defined(MQTT_USERNAME) && defined(MQTT_PASSWORD)
    state.mqtt_client_info.client_user = MQTT_USERNAME;
    state.mqtt_client_info.client_pass = MQTT_PASSWORD;
#else
    state.mqtt_client_info.client_user = NULL;
    state.mqtt_client_info.client_pass = NULL;
#endif
    static char will_topic[MQTT_TOPIC_LEN];
    strncpy(will_topic, full_topic(&state, MQTT_WILL_TOPIC), sizeof(will_topic));
    state.mqtt_client_info.will_topic = will_topic;
    state.mqtt_client_info.will_msg = MQTT_WILL_MSG;
    state.mqtt_client_info.will_qos = MQTT_WILL_QOS;
    state.mqtt_client_info.will_retain = true;
    client_state = &state;
#if LWIP_ALTCP && LWIP_ALTCP_TLS
    // TLS enabled
#ifdef MQTT_CERT_INC
    static const uint8_t ca_cert[] = TLS_ROOT_CERT;
    static const uint8_t client_key[] = TLS_CLIENT_KEY;
    static const uint8_t client_cert[] = TLS_CLIENT_CERT;
    // This confirms the indentity of the server and the client
    state.mqtt_client_info.tls_config = altcp_tls_create_config_client_2wayauth(ca_cert, sizeof(ca_cert),
            client_key, sizeof(client_key), NULL, 0, client_cert, sizeof(client_cert));
#if ALTCP_MBEDTLS_AUTHMODE != MBEDTLS_SSL_VERIFY_REQUIRED
    WARN_printf("Warning: tls without verification is insecure\n");
#endif
#else
    state->client_info.tls_config = altcp_tls_create_config_client(NULL, 0);
    WARN_printf("Warning: tls without a certificate is insecure\n");
#endif
#endif

    cyw43_arch_enable_sta_mode();
    if (cyw43_arch_wifi_connect_timeout_ms(WIFI_SSID, WIFI_PASSWORD, CYW43_AUTH_WPA2_AES_PSK, 30000)) {
        panic("Failed to connect");
    }
    INFO_printf("\nConnected to Wifi\n");

    // We are not in a callback so locking is needed when calling lwip
    // Make a DNS request for the MQTT server IP address
    cyw43_arch_lwip_begin();
    int err = dns_gethostbyname(MQTT_SERVER, &state.mqtt_server_address, dns_found, &state);
    cyw43_arch_lwip_end();

    if (err == ERR_OK) {
        // We have the address, just start the client
        start_client(&state);
    } else if (err != ERR_INPROGRESS) { // ERR_INPROGRESS means expect a callback
        panic("dns request failed");
    }

    while (!state.connect_done || mqtt_client_is_connected(state.mqtt_client_inst)) {
        cyw43_arch_poll();
        cyw43_arch_wait_for_work_until(make_timeout_time_ms(10000));
    }

    INFO_printf("mqtt client exiting\n");
    return 0;
}
