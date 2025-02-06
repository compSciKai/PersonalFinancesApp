using Microsoft.AspNetCore.Mvc;
using PersonalFinancesApp.Api.Data;
using PersonalFinancesApp.Api.Models;

namespace PersonalFinancesApp.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FinancesController : ControllerBase
{
    DataContextEF _entityFramework;
    public FinancesController(IConfiguration config)
    {
        _entityFramework = new DataContextEF(config);
    }

    [HttpGet("GetUsers")]
    public IEnumerable<User> GetFinances()
    {
        IEnumerable<User> users = _entityFramework.Users.ToList<User>();
        return users;
    }
}
