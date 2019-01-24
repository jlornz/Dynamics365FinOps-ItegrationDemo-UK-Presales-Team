using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DemoWebApplication.Pages
{
    public class Demo3tModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "This demo tree";
        }
    }
}
