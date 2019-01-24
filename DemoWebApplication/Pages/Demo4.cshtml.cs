using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DemoWebApplication.Pages
{
    public class Demo4Model : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "This is demo four";
        }
    }
}
