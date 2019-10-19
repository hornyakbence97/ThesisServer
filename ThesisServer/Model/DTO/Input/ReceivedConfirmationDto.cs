using System;

namespace ThesisServer.Model.DTO.Input
{
    public class ReceivedConfirmationDto : BaseDto
    {
        public Guid ReceiveId { get; set; }
        public ConfirmationType Type { get; set; }
        public long FilePeaceSize { get; set; }
    }
}
