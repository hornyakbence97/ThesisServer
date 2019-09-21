using UnitTests;

namespace Tests
{
    public class TestBase
    {
        protected TestApi _api;

        protected TestBase()
        {
            _api = new TestApi();
        }
    }
}
