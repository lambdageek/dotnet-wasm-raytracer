using System.Runtime.InteropServices.JavaScript;
using System;
using RayTracer;
using System.Threading.Tasks;
using System.Runtime.Intrinsics;

public partial class MainJS
{
    struct SceneEnvironment {
        public int Width;
        public int Height;
        public byte[] rgbaRenderBuffer;
        public Scene Scene;
        public RayTracer.Objects.DrawableSceneObject YellowSphere;
        public double worldClockNow;
    }

    static SceneEnvironment sceneEnvironment;

    public static void Main()
    {
        Console.WriteLine ("Hello, World!");
    }

    internal static Scene ConfigureScene ()
    {
        var scene = Scene.TwoPlanes;
        scene.Camera.ReflectionDepth = 5;
        scene.Camera.FieldOfView = 120;
        return scene;
    }

    [JSExport]
    [return: JSMarshalAs<JSType.MemoryView>]
    internal static ArraySegment<byte> PrepareToRender(int sceneWidth, int sceneHeight)
    {
        sceneEnvironment.Width = sceneWidth;
        sceneEnvironment.Height = sceneHeight;
        sceneEnvironment.Scene = ConfigureScene();
        sceneEnvironment.YellowSphere = sceneEnvironment.Scene.DrawableObjects[2];
        sceneEnvironment.rgbaRenderBuffer = new byte[sceneWidth * sceneHeight * 4];
        sceneEnvironment.worldClockNow = 0.0;
        return sceneEnvironment.rgbaRenderBuffer;
    }

    [JSImport("renderCanvas", "main.js")]
    internal static partial void RenderCanvas();


    [JSImport("setOutText", "main.js")]
    internal static partial void SetOutText(string text);

    [JSExport]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    internal static async Task OnClick(){
        var now = DateTime.UtcNow;
        string text;
        text = "Rendering started";
#if !USE_THREADS
        Console.WriteLine(text);
#endif
        SetOutText(text);

        await sceneEnvironment.Scene.Camera.RenderScene(sceneEnvironment.Scene, sceneEnvironment.rgbaRenderBuffer, sceneEnvironment.Width, sceneEnvironment.Height);

        text = $"Rendering finished in {(DateTime.UtcNow - now).TotalMilliseconds} ms";
#if !USE_THREADS
        Console.WriteLine(text);
#endif
        SetOutText(text);
        RenderCanvas();
        AdjustSceneObjects ();
    }

    public static void AdjustSceneObjects ()
    {
        sceneEnvironment.worldClockNow += 0.1;
        sceneEnvironment.YellowSphere.Position = RepositionYellowSphere (sceneEnvironment.YellowSphere.Position, sceneEnvironment.worldClockNow);
    }

    public static Vector128<float> RepositionYellowSphere (Vector128<float> position, double worldClockNow)
    {
        // wiggle around from side to side
        float newX = -5.0f + (float)(5.0 * Math.Sin(Math.PI * (worldClockNow / 0.5)));
        return Vector128.WithElement(position, 0, newX);
    }
}
