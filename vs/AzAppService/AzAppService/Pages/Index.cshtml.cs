using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace AzAppService.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var path = Path.Combine(
                @"d:\\home",
                "data.txt");

            ViewData["path"] = path;

            if (System.IO.File.Exists(path))
                ViewData["data"] = System.IO.File.ReadAllText(path);
        }
    }
}
