using Microsoft.AspNetCore.Mvc;

namespace Hlouwa.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SpaFallbackController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SpaFallbackController> _logger;

        public SpaFallbackController(IWebHostEnvironment env, ILogger<SpaFallbackController> logger)
        {
            _env = env;
            _logger = logger;
        }

        // Cette action sera appelée par:
        // app.MapFallbackToController("{*path:nonfile}", "Index", "SpaFallback");
        [HttpGet]
        public IActionResult Index(string? path)
        {
            // ✅ Ne jamais servir index.html pour /api/*
            // plus fiable que path.StartsWith("api") car path peut être null
            if (Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            // (Optionnel) ne pas casser swagger si jamais activé en prod
            if (Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            // ✅ IIS-safe: WebRootPath (wwwroot) sinon fallback ContentRootPath/wwwroot
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            var indexPath = Path.Combine(webRoot, "index.html");

            if (!System.IO.File.Exists(indexPath))
            {
                _logger.LogWarning("SPA index.html introuvable: {IndexPath}", indexPath);
                return NotFound("index.html introuvable dans wwwroot. Vérifie ton build Angular.");
            }

            return PhysicalFile(indexPath, "text/html; charset=utf-8");
        }
    }
}
