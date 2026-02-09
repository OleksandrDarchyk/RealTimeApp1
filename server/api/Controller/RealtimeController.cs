using System.Text.Json;
using api.dto;
using dataccess;
using dataccess.Entities;
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

        // 1) Notify the room that someone joined
        await backplane.Clients.SendToGroupAsync(roomId, new
        {
            eventType = "SystemMessage",
            message = $"someone entered {roomId}",
            kind = "join"
        });

        // 2) Add the client connection to the room group
        await backplane.Groups.AddToGroupAsync(req.ConnectionId, roomId);

        var last = await ctx.Messages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new MessageDto(m.Id, m.Content, m.From, m.CreatedAt))
            .ToListAsync(HttpContext.RequestAborted);

        last.Reverse();

        // Send room history only to the joining client (direct message; evt.Group is null -> mapped to "direct" in /connect)
        await backplane.Clients.SendToClientAsync(
            req.ConnectionId,
            new RoomHistoryResponse(roomId, last)
        );

        return NoContent();
    }

    [HttpPost("rooms/{roomId}/messages")]
    public async Task<IActionResult> SendMessage([FromRoute] string roomId, [FromBody] SendMessageRequest req)
    {
        var roomExists = await ctx.Rooms.AnyAsync(r => r.Id == roomId, HttpContext.RequestAborted);
        if (!roomExists)
            return BadRequest($"Room '{roomId}' does not exist");

        // 1) Persist the message in the database
        var msg = new Message
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Content = req.Content,
            From = req.From,
            CreatedAt = DateTimeOffset.UtcNow
        };

        ctx.Messages.Add(msg);
        await ctx.SaveChangesAsync(HttpContext.RequestAborted);

        // 2) Broadcast the message to the room
        await backplane.Clients.SendToGroupAsync(roomId, new
        {
            message = req.Content,
            from = req.From,
            eventType = "messageHasBeenReceived"
        });

        return NoContent();
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<IActionResult> GetMessages([FromRoute] string roomId, [FromQuery] int limit = 5)
    {
        // 1) Protect against invalid limit values
        if (limit < 1) limit = 1;
        if (limit > 50) limit = 50;

        // 2) Room must exist
        var roomExists = await ctx.Rooms.AnyAsync(r => r.Id == roomId, HttpContext.RequestAborted);
        if (!roomExists)
            return BadRequest($"Room '{roomId}' does not exist");

        // 3) Read last N messages (descending) and map to DTO
        var last = await ctx.Messages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new MessageDto(m.Id, m.Content, m.From, m.CreatedAt))
            .ToListAsync(HttpContext.RequestAborted);

        // 4) Reverse so UI can render oldest -> newest
        last.Reverse();

        return Ok(last);
    }

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

        return NoContent();
    }

    [HttpPost("CreateRooms")]
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
}
