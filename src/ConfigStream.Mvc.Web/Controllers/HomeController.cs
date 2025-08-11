using ConfigStream.Core.Interfaces;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConfigStream.Mvc.Web.Models;

namespace ConfigStream.Mvc.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfigurationReader _configurationReader;

    public HomeController(ILogger<HomeController> logger, IConfigurationReader configurationReader)
    {
        _logger = logger;
        _configurationReader = configurationReader;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Configurations()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
