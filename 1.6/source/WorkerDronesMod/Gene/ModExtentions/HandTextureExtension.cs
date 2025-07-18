using Verse;

namespace WorkerDronesMod
{
    public class HandTextureExtension : DefModExtension
    {
        public string mainTexturePath;
        public string mainCleanTexturePath;
        public string offTexturePath;
        public string offCleanTexturePath;
        public ShaderTypeDef shaderType;
        public string mainMaskPath;   // <— the cutout mask for your main‑hand
        public string offMaskPath;    // <— the cutout mask for your off‑hand
    }
}
