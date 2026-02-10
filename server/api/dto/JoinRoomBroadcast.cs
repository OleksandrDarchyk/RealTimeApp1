using StateleSSE.AspNetCore;

namespace api.dto;

public record JoinRoomBroadcast(List<ConnectionIdAndUserName> ConnectedUsers) : BaseResponseDto;
