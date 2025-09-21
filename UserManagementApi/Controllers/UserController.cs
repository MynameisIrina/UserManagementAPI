using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{

    // In-memory list of users
    private static List<User> Users = new List<User>
    {
        new User { Id = 1, Name = "Alice" },
        new User { Id = 2, Name = "Bob" }
    };

    // GET: /User
    [HttpGet]
    public ActionResult<IEnumerable<User>> Get()
    {
        return Ok(Users);
    }

    // GET: /User/{id}
    [HttpGet("{id}")]
    public ActionResult<User> Get(int id)
    {
        try
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            // Log the exception as needed
            return StatusCode(500, new { error = "An error occurred while retrieving the user." });
        }
    }

    // POST: /User
    [HttpPost]
    public ActionResult<User> Post([FromBody] User user)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (Users.Any(u => u.Id == user.Id))
                return Conflict("User with this ID already exists.");

            Users.Add(user);
            return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            // Log the exception as needed
            return StatusCode(500, new { error = "An error occurred while creating the user." });
        }
    }

    // PUT: /User/{id}
    [HttpPut("{id}")]
    public ActionResult<User> Put(int id, [FromBody] User updatedUser)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            user.Name = updatedUser.Name;
            return Ok(user);
        }
        catch (Exception ex)
        {
            // Log the exception as needed
            return StatusCode(500, new { error = "An error occurred while updating the user." });
        }
    }

    // DELETE: /User/{id}
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        try
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            Users.Remove(user);
            return NoContent();
        }
        catch (Exception ex)
        {
            // Log the exception as needed
            return StatusCode(500, new { error = "An error occurred while deleting the user." });
        }
    }
}