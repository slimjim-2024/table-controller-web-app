using asp_net_core.Areas.Identity.Pages.Account;
using asp_net_core.Data;
using asp_net_core.Models;
using asp_net_core.Services;
using asp_net_core.Views.Home;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace WebTests.Models
{
    public class DeskHeightRangeFormTests
    {
        private readonly ManageModel _manageModel;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ConfigService> _configServiceMock;

        public DeskHeightRangeFormTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new ApplicationDbContext(options);

            // Setup UserManager mock
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            _configServiceMock = new Mock<ConfigService>();

            // Create actual instance (not mock) so we can test ModelState
            _manageModel = new ManageModel(_dbContext, _userManagerMock.Object, _configServiceMock.Object);
        }


        [Fact]
        public void DeskHeightRangeForm_SetsPropertiesCorrectly()
        {
            var form = new Dashboard_component.DeskHeightRangeForm
            {
                LowerHeight = 680,
                UpperHeight = 1320
            };
            Assert.Equal(680, form.LowerHeight);
            Assert.Equal(1320, form.UpperHeight);
        }

        [Theory]
        [InlineData(680, 1320)]
        [InlineData(800, 1200)]
        [InlineData(700, 1000)]

        public void DeskHeightRangeForm_AcceptsValidHeights(int lower, int upper)
        {
            var form = new HeightChangeForm
            {
                LowerHeight = lower,
                UpperHeight = upper
            };
            
            
            Assert.False(_manageModel.HasErrors(form));
        }

        [Theory]
        [InlineData(1320, 680)] // Lower greater than upper
        [InlineData(600, 1320)] // Lower below min
        [InlineData(680, 1400)] // Upper above max
        public void DeskHeightRangeForm_RejectsInvalidHeights(int lower, int upper)
        {
            var form = new HeightChangeForm
            {
                LowerHeight = lower,
                UpperHeight = upper
            };
            
            
            Assert.True(_manageModel.HasErrors(form));
        }
    }
}