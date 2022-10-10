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
        public RayTracer.Camera Camera;
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
        sceneEnvironment.Camera = sceneEnvironment.Scene.Camera;
        return sceneEnvironment.rgbaRenderBuffer;
    }

    [JSImport("renderCanvas", "main.js")]
    internal static partial void RenderCanvas();


    [JSImport("setOutText", "main.js")]
    internal static partial void SetOutText(string text);

    static int totalFrames = 0;
    static double totalRenderTime = 0.0;

    [JSExport]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    internal static async Task OnClick(){
        var now = DateTime.UtcNow;
        string text;
        text = "Rendering started";
#if !USE_THREADS
        //Console.WriteLine(text);
#endif
        //SetOutText(text);

        await sceneEnvironment.Scene.Camera.RenderScene(sceneEnvironment.Scene, sceneEnvironment.rgbaRenderBuffer, sceneEnvironment.Width, sceneEnvironment.Height);

        var elapsedMs = (DateTime.UtcNow - now).TotalMilliseconds;
        totalFrames++;
        totalRenderTime += elapsedMs;

        var avgTime = totalRenderTime / totalFrames;

        text = $"Rendering finished in {elapsedMs} ms; average time: {avgTime} ms";
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
        sceneEnvironment.Camera.Position = RepositionCamera(sceneEnvironment.Camera.Position, sceneEnvironment.worldClockNow);
        sceneEnvironment.Camera.LookAt(Vector128.Create(0f, -.3f, 1.0f, 0));
    }

    public static Vector128<float> RepositionYellowSphere (Vector128<float> position, double worldClockNow)
    {
        // wiggle around from side to side
        float newX = -5.0f + (float)(5.0 * Math.Sin(Math.PI * (worldClockNow / 0.5)));
        return Vector128.WithElement(position, 0, newX);
    }

    public static Vector128<float> RepositionCamera(Vector128<float> position, double worldClockNow)
    {
        //Vector128<float> initialPosition = Vector128.Create(0f, 2f, -2f, 0);
        double timeParam = Math.PI * (worldClockNow / 4.0);
        Vector128<float> newPosition = Vector128.Create(0f + (float)(20.0 * Math.Sin(timeParam)), 2f, 18.0f + (float)(20.0 * Math.Cos(Math.PI + timeParam)), 0);
        return newPosition;
    }
}
