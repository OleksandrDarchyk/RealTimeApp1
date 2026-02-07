using System.Text.Json;
using api.dto;
using Microsoft.AspNetCore.Mvc;
using StateleSSE.AspNetCore;


[ApiController]
[Route("")]
public class RealtimeController(ISseBackplane backplane) : ControllerBase
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
        // 1) give a message who in a room
        await backplane.Clients.SendToGroupAsync(roomId, new
        {
            eventType = "SystemMessage",
            message = $"someone entered {roomId}",
            kind = "join"
        });

        // 2) add new client to the group
        await backplane.Groups.AddToGroupAsync(req.ConnectionId, roomId);

        return NoContent();
    }


 
   
    
    
    [HttpPost("rooms/{roomId}/messages")]
    public async Task<IActionResult> SendMessage([FromRoute] string roomId, [FromBody] SendMessageRequest req)
    {
        await backplane.Clients.SendToGroupAsync(roomId, new
        {
            message = req.Content,
            from = req.From,
            eventType = "messageHasBeenReceived"
        });

        return NoContent();
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




    // [HttpPost("join")]
    // public async Task Join(string connectionId, string room)
    //     => await backplane.Groups.AddToGroupAsync(connectionId, room);
    //
    
    //Potentially useful for testing, but not needed for the actual app since clients will send messages directly to groups instead of the server sending them to groups.
    // [HttpPost("send")]
    // public async Task Send(string room, string message)
    //     => await backplane.Clients.SendToGroupAsync(room, new
    //     {
    //         message ,
    //         eventType = "messageHasBeenReceived"
    //     });
    //
    
    
    // [HttpPost("rooms/{roomId}/join")]
    // public async Task<IActionResult> JoinRoom([FromRoute] string roomId, [FromBody] ConnectionRequest req)
    // {
    //     await backplane.Groups.AddToGroupAsync(req.ConnectionId, roomId);
    //     return NoContent();
    // }
    //
    
    // [HttpPost("rooms/{roomId}/leave")]
    // public async Task<IActionResult> LeaveRoom([FromRoute] string roomId, [FromBody] ConnectionRequest req)
    // {
    //     await backplane.Groups.RemoveFromGroupAsync(req.ConnectionId, roomId);
    //     return NoContent();
    // }
}