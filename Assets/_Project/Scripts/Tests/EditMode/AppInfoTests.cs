using LotoSim.Core;
using NUnit.Framework;

namespace LotoSim.Tests
{
    public class AppInfoTests
    {
        [Test]
        public void ProductName_IsDefined_NotEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(AppInfo.ProductName));
        }
    }
}
