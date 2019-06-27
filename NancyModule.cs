using Nancy;

namespace NancyStandalone
{
  public class HelloModule : NancyModule
  {
    public HelloModule()
    {
      Get("/nancy", args => { return "Hello World from Nancy"; });
    }
  }
}
