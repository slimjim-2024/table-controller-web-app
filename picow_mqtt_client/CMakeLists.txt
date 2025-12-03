# Generated Cmake Pico project file

cmake_minimum_required(VERSION 3.13)

set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Initialise pico_sdk from installed location
# (note this can come from environment, CMake cache etc)

# == DO NOT EDIT THE FOLLOWING LINES for the Raspberry Pi Pico VS Code Extension to work ==
if(WIN32)
    set(USERHOME $ENV{USERPROFILE})
else()
    set(USERHOME $ENV{HOME})
endif()
set(sdkVersion 2.2.0)
set(toolchainVersion 14_2_Rel1)
set(picotoolVersion 2.2.0-a4)
set(picoVscode ${USERHOME}/.pico-sdk/cmake/pico-vscode.cmake)
if (EXISTS ${picoVscode})
    include(${picoVscode})
endif()
# ====================================================================================
set(PICO_BOARD pico_w CACHE STRING "Board type")

# Pull in Raspberry Pi Pico SDK (must be before project)
include(pico_sdk_import.cmake)

project(picow_mqtt_client C CXX ASM)

set(MQTT_SERVER "10.151.252.201") # Change this to the host name of your MQTT server

set(WIFI_SSID "Vlad's Galaxy S23+")
set(WIFI_PASSWORD "superhacks")
set(MQTT_USERNAME "PICOUSER")
set(MQTT_PASSWORD "PicoPass1234!")

# Initialise the Raspberry Pi Pico SDK
pico_sdk_init()

# Add executable. Default name is the project name, version 0.1

# Define the host name of the MQTT server in an environment variable or pass it to cmake,
# e.g. cmake -DMQTT_SERVER=myserver ..

if (DEFINED ENV{MQTT_SERVER} AND (NOT MQTT_SERVER))
    set(MQTT_SERVER $ENV{MQTT_SERVER})
    message("Using MQTT_SERVER from environment ('${MQTT_SERVER}')")
endif()
if (NOT MQTT_SERVER)
    message("Skipping MQTT example as MQTT_SERVER is not defined")
    return()
endif()
# Define the name of an MQTT broker/server to enable this example
set(MQTT_SERVER "${MQTT_SERVER}" CACHE INTERNAL "MQTT server for examples")

if (DEFINED ENV{MQTT_USERNAME} AND (NOT MQTT_USERNAME))
    set(MQTT_USERNAME $ENV{MQTT_USERNAME})
    message("Using MQTT_USERNAME from environment ('${MQTT_USERNAME}')")
endif()
set(MQTT_USERNAME "${MQTT_USERNAME}" CACHE INTERNAL "MQTT user name for examples")
if (DEFINED ENV{MQTT_PASSWORD} AND (NOT MQTT_PASSWORD))
    set(MQTT_PASSWORD $ENV{MQTT_PASSWORD})
    message("Using MQTT_PASSWORD from environment")
endif()
set(MQTT_PASSWORD "${MQTT_PASSWORD}" CACHE INTERNAL "MQTT password for examples")

# Set path to the certificate include file
if (NOT MQTT_CERT_PATH)
    set(MQTT_CERT_PATH ${CMAKE_CURRENT_LIST_DIR}/certs/${MQTT_SERVER})
endif()

# Set the name of the certificate include file
if (NOT MQTT_CERT_INC)
    set(MQTT_CERT_INC mqtt_client.inc)
endif()

add_executable(picow_mqtt_client
    mqtt_client.c
    )
target_link_libraries(picow_mqtt_client
    pico_stdlib
    hardware_adc
    pico_cyw43_arch_lwip_threadsafe_background
    pico_lwip_mqtt
    pico_mbedtls
    pico_lwip_mbedtls
    )
target_include_directories(picow_mqtt_client PRIVATE
    ${CMAKE_CURRENT_LIST_DIR}
    ${CMAKE_CURRENT_LIST_DIR}/.. # for our common lwipopts or any other standard includes, if required
    )
target_compile_definitions(picow_mqtt_client PRIVATE
    WIFI_SSID=\"${WIFI_SSID}\"
    WIFI_PASSWORD=\"${WIFI_PASSWORD}\"
    MQTT_SERVER=\"${MQTT_SERVER}\"
    )
if (EXISTS "${MQTT_CERT_PATH}/${MQTT_CERT_INC}")
    target_compile_definitions(picow_mqtt_client PRIVATE
        MQTT_CERT_INC=\"${MQTT_CERT_INC}\" # contains the tls certificates for MQTT_SERVER needed by the client
        ALTCP_MBEDTLS_AUTHMODE=MBEDTLS_SSL_VERIFY_REQUIRED
        )
    target_include_directories(picow_mqtt_client PRIVATE
        ${MQTT_CERT_PATH}
        )
endif()
if (MQTT_USERNAME AND MQTT_PASSWORD)
    target_compile_definitions(picow_mqtt_client PRIVATE
        MQTT_USERNAME=\"${MQTT_USERNAME}\"
        MQTT_PASSWORD=\"${MQTT_PASSWORD}\"
    )
endif()
pico_add_extra_outputs(picow_mqtt_client)

# Ignore warnings from lwip code
set_source_files_properties(
    ${PICO_LWIP_PATH}/src/apps/altcp_tls/altcp_tls_mbedtls.c
    PROPERTIES
    COMPILE_OPTIONS "-Wno-unused-result"
    )