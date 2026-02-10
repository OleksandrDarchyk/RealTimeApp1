using System.Security.Claims;
using System.Text.Json;
using api.dto;
using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StateleSSE.AspNetCore;

[ApiController]
[Route("")]
public class RealtimeController(ISseBackplane backplane, ChatDbContext ctx) : ControllerBase
{
    [HttpGet("connect")]
    public async Task Connect()
    {
        await using var sse = await HttpContext.OpenSseStreamAsync();
        await using var connection = backplane.CreateConnection();

        await sse.WriteAsync(
            "ConnectionResponse",
            JsonSerializer.Serialize(
                new
                {
                    eventType = "ConnectionResponse",
                    connectionId = connection.ConnectionId
                },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            )
        );

        await foreach (var evt in connection.ReadAllAsync(HttpContext.RequestAborted))
        {
            await sse.WriteAsync(evt.Group ?? "direct", evt.Data);
        }
    }

    [HttpPost("rooms/{roomId}/join")]
    public async Task<IActionResult> JoinRoom([FromRoute] string roomId, [FromBody] ConnectionRequest req)
    {
        var roomExists = await ctx.Rooms.AnyAsync(r => r.Id == roomId, HttpContext.RequestAborted);
        if (!roomExists)
            return BadRequest($"Room '{roomId}' does not exist");

        // Resolve nickname for this request (if user is authenticated -> use DB nickname, else Anonymous)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string nickname;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == userId, HttpContext.RequestAborted);
            nickname = user?.Nickname ?? "Anonymous";
        }
        else
        {
            nickname = "Anonymous";
        }

        // Store nickname in Redis/backplane so we can resolve it later by connectionId (teacher-style trick)
        // "nickname/<connectionId>" is treated like a client id, and we "join" it to a group named "<nickname>"
        await backplane.Groups.AddToGroupAsync($"nickname/{req.ConnectionId}", nickname);

        // Notify the room that someone joined (broadcast)
        await backplane.Clients.SendToGroupAsync(roomId, new
        {
            eventType = "SystemMessage",
            message = $"someone entered {roomId}",
            kind = "join"
        });

        // Add the client connection to the room group
        await backplane.Groups.AddToGroupAsync(req.ConnectionId, roomId);

        // Build and broadcast members list to everyone in the room
        var members = await backplane.Groups.GetMembersAsync(roomId);
        var list = new List<ConnectionIdAndUserName>();

        foreach (var m in members)
        {
            var nickGroups = await backplane.Groups.GetClientGroupsAsync($"nickname/{m}");
            var nick = nickGroups.FirstOrDefault() ?? "Anonymous";
            list.Add(new ConnectionIdAndUserName(m, nick));
        }

        await backplane.Clients.SendToGroupAsync(roomId, new JoinRoomBroadcast(list));

        // Load last 5 messages from DB and send ONLY to the joining client (direct)
        var last = await ctx.Messages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new MessageDto(m.Id, m.Content, m.From, m.CreatedAt))
            .ToListAsync(HttpContext.RequestAborted);

        last.Reverse();

        await backplane.Clients.SendToClientAsync(
            req.ConnectionId,
            new RoomHistoryResponse(roomId, last)
        );

        return NoContent();
    }

    [Authorize]
    [HttpPost("rooms/{roomId}/messages")]
    public async Task<IActionResult> SendMessage([FromRoute] string roomId, [FromBody] SendMessageRequest req)
    {
        var roomExists = await ctx.Rooms.AnyAsync(r => r.Id == roomId, HttpContext.RequestAborted);
        if (!roomExists)
            return BadRequest($"Room '{roomId}' does not exist");

        // Get userId from JWT (NameIdentifier claim)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        // Resolve nickname from DB
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == userId, HttpContext.RequestAborted);
        if (user is null)
            return Unauthorized();

        var nickname = user.Nickname;

        // Persist the message in the database
        var msg = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Content = req.Content,
            From = nickname,
            CreatedAt = DateTimeOffset.UtcNow
        };

        ctx.Messages.Add(msg);
        await ctx.SaveChangesAsync(HttpContext.RequestAborted);

        // Broadcast the message to the room
        await backplane.Clients.SendToGroupAsync(roomId, new
        {
            message = req.Content,
            from = nickname,
            eventType = "messageHasBeenReceived"
        });

        return NoContent();
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<IActionResult> GetMessages([FromRoute] string roomId, [FromQuery] int limit = 5)
    {
        if (limit < 1) limit = 1;
        if (limit > 50) limit = 50;

        var roomExists = await ctx.Rooms.AnyAsync(r => r.Id == roomId, HttpContext.RequestAborted);
        if (!roomExists)
            return BadRequest($"Room '{roomId}' does not exist");

        var last = await ctx.Messages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new MessageDto(m.Id, m.Content, m.From, m.CreatedAt))
            .ToListAsync(HttpContext.RequestAborted);

        last.Reverse();
        return Ok(last);
    }

    [Authorize]
    [HttpPost("poke")]
    public async Task<IActionResult> Poke([FromBody] PokeRequest req)
    {
        await backplane.Clients.SendToClientAsync(req.TargetConnectionId, new
        {
            eventType = "PokeResponse",
            message = "you have been poked"
        });

        return NoContent();
    }

    [HttpPost("rooms/{roomId}/leave")]
    public async Task<IActionResult> LeaveRoom([FromRoute] string roomId, [FromBody] ConnectionRequest req)
    {
        await backplane.Groups.RemoveFromGroupAsync(req.ConnectionId, roomId);

        await backplane.Clients.SendToGroupAsync(roomId, new
        {
            eventType = "SystemMessage",
            message = $"someone left {roomId}",
            kind = "leave"
        });

        // Optional: also broadcast updated members list (same as teacher's join approach, but for leave)
        var members = await backplane.Groups.GetMembersAsync(roomId);
        var list = new List<ConnectionIdAndUserName>();

        foreach (var m in members)
        {
            var nickGroups = await backplane.Groups.GetClientGroupsAsync($"nickname/{m}");
            var nick = nickGroups.FirstOrDefault() ?? "Anonymous";
            list.Add(new ConnectionIdAndUserName(m, nick));
        }

        await backplane.Clients.SendToGroupAsync(roomId, new JoinRoomBroadcast(list));

        return NoContent();
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest req)
    {
        var exists = await ctx.Rooms.AnyAsync(r => r.Id == req.RoomId, HttpContext.RequestAborted);
        if (exists) return Conflict($"Room '{req.RoomId}' already exists");

        ctx.Rooms.Add(new dataccess.Entities.Room
        {
            Id = req.RoomId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await ctx.SaveChangesAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<List<RoomDto>>> GetRooms()
    {
        var rooms = await ctx.Rooms
            .OrderBy(r => r.CreatedAt)
            .Select(r => new RoomDto(r.Id, r.CreatedAt))
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(rooms);
    }
}
