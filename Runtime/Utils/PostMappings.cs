using System.Runtime.Serialization;

namespace PostLib
{
    public enum PostMappings
    {
        //Basic
        [EnumMember(Value = "ucip.basic.g2wInitializationRequest")] InitializationRequest,
        [EnumMember(Value = "ucip.basic.g2wCloseGameFrameCommand")] CloseGameHandler,
        [EnumMember(Value = "ucip.basic.w2gInitializationResponse")] InitializationResponse,
        //Pause
        [EnumMember(Value = "ucip.pause.w2gPauseCommand")] GamePauseHandler,
        [EnumMember(Value = "ucip.pause.g2wPauseNotification")] PauseNotification,
        //AutoPlay
        [EnumMember(Value = "ucip.autoplay.g2wAutoplayStartNotification")] AutoPlayStartNotification,
        [EnumMember(Value = "ucip.autoplay.g2wAutoplayEndNotification")] AutoPlayEndNotification,
        [EnumMember(Value = "ucip.autoplay.w2gInterruptGameplayCommand")] AutoPlayInterruptCommand,
    }
}