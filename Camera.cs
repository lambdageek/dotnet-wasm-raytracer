﻿using RayTracer.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;

namespace RayTracer
{
    /// <summary>
    /// The scene camera, contains all relevant rendering methods and algorithms.
    /// </summary>
    public class Camera : SceneObjectBase
    {
        private Vector3 forward, up, right;
        private Vector3 screenPosition;
        private float fieldOfView;
        public float FieldOfView
        {
            get { return fieldOfView; }
            set
            {
                fieldOfView = value;
                RecalculateFieldOfView();
            }
        }
        private float yRatio;

        public int ReflectionDepth { get; set; }
        private Size renderSize;
        public Size RenderSize { get { return renderSize; } set { renderSize = value; OnRenderSizeChanged(); } }

        private void OnRenderSizeChanged()
        {
            this.yRatio = (float)renderSize.Height / (float)renderSize.Width;
        }

        public Camera() : this(Vector3.Zero, Util.ForwardVector, Util.UpVector, 70f, new Size(640, 480)) { }

        public Camera(Vector3 position, Vector3 forward, Vector3 worldUp, float fieldOfView, Size renderSize)
            : base(position)
        {
            this.ReflectionDepth = 5;
            this.forward = forward.Normalized();
            this.right = Util.CrossProduct(worldUp, forward).Normalized();
            this.up = -Util.CrossProduct(right, forward).Normalized();
            this.fieldOfView = fieldOfView;
            this.RenderSize = renderSize;

            RecalculateFieldOfView();
        }

        private void RecalculateFieldOfView()
        {
            var screenDistance = 1f / (float)Math.Tan(Util.DegreesToRadians(fieldOfView) / 2f);

            this.screenPosition = this.Position + forward * new Vector3(screenDistance);
        }

        private Ray GetRay(float viewPortX, float viewPortY)
        {
            var rayWorldPosition = screenPosition + ((new Vector3(viewPortX) * right) + (new Vector3(viewPortY) * up * new Vector3(yRatio)));
            var direction = rayWorldPosition - this.Position;
            return new Ray(rayWorldPosition, direction);
        }

        private Ray GetReflectionRay(Vector3 origin, Vector3 normal, Vector3 impactDirection)
        {
            float c1 = Vector3.Dot(-normal, impactDirection);
            Vector3 reflectionDirection = impactDirection + (normal * new Vector3(2 * c1));
            return new Ray(origin + reflectionDirection * new Vector3(.01f), reflectionDirection); // Ensures the ray starts "just off" the reflected surface
        }

        private Ray GetRefractionRay(Vector3 origin, Vector3 normal, Vector3 previousDirection, float refractivity)
        {
            float c1 = Vector3.Dot(normal, previousDirection);
            float c2 = 1 - refractivity * refractivity * (1 - c1 * c1);
            if (c2 < 0)
                c2 = (float)Math.Sqrt(c2);
            Vector3 refractionDirection = (normal * new Vector3((refractivity * c1 - c2)) - previousDirection * new Vector3(refractivity)) * new Vector3(-1);
            return new Ray(origin, refractionDirection.Normalized()); // no refraction
        }

        /// <summary>
        /// Renders the given scene in a background thread. Uses a single thread for rendering.
        /// </summary>
        /// <param name="scene">The scene to render</param>
        /// <returns>A bitmap of the rendered scene.</returns>
        public byte[] RenderScene(Scene scene, int width = -1, int height = -1)
        {
            if (width == -1 || height == -1)
            {
                width = renderSize.Width;
                height = renderSize.Height;
            }
            else
            {
                renderSize = new Size(width, height);
            }

            var rgbaBytes = new byte[height * width * 4];

	    Task[] renderer = new Task[4];
	    renderer[0] = Task.Run (() => {
		    RenderRange (scene, width, height, rgbaBytes, 0, width/2, 0, height/2);
	    });
	    renderer[1] = Task.Run (() => {
		    RenderRange (scene, width, height, rgbaBytes, width/2, width, 0, height/2);
	    });
	    renderer[2] = Task.Run (() => {
		    RenderRange (scene, width, height, rgbaBytes, 0, width/2, height/2, height);
	    });
	    renderer[3] = Task.Run (() => {
		    RenderRange (scene, width, height, rgbaBytes, width/2, width, height/2, height);
	    });

	    Task.WaitAll(renderer);

            return rgbaBytes;
        }

	private void RenderRange (Scene scene, int width, int height, byte[] rgbaBytes, int xStart, int xEnd, int yStart, int yEnd)
	{
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    var viewPortX = ((2 * x) / (float)width) - 1;
                    var viewPortY = ((2 * y) / (float)height) - 1;
                    var color = TraceRayAgainstScene(GetRay(viewPortX, viewPortY), scene);

                    var red = 4 * (width * (height - y - 1) + x);
                    rgbaBytes[red] = (byte)(color.R * 256);
                    rgbaBytes[red + 1] = (byte)(color.G * 256);
                    rgbaBytes[red + 2] = (byte)(color.B * 256);
                    rgbaBytes[red + 3] = 255;
                }
            }
	}

        private Color TraceRayAgainstScene(Ray ray, Scene scene)
        {
            Intersection intersection;
            if (TryCalculateIntersection(ray, scene, null, out intersection))
            {
                return CalculateRecursiveColor(intersection, scene, 0);
            }
            else
            {
                return scene.BackgroundColor;
            }
        }

        /// <summary>
        /// Recursive algorithm base
        /// </summary>
        /// <param name="intersection">The intersection the recursive step started from</param>
        /// <param name="ray">The ray, starting from the intersection</param>
        /// <param name="scene">The scene to trace</param>
        private Color CalculateRecursiveColor(Intersection intersection, Scene scene, int depth)
        {
            // Ambient light:
            var color = Color.Lerp(Color.Black, intersection.Color * scene.AmbientLightColor, scene.AmbientLightIntensity);

            foreach (Light light in scene.Lights)
            {
                var lightContribution = new Color();
                var towardsLight = (light.Position - intersection.Point).Normalized();
                var lightDistance = Util.Distance(intersection.Point, light.Position);

                // Accumulate diffuse lighting:
                var lightEffectiveness = Vector3.Dot(towardsLight, intersection.Normal);
                if (lightEffectiveness > 0.0f)
                {
                    lightContribution = lightContribution + (intersection.Color * light.GetIntensityAtDistance(lightDistance) * light.Color * lightEffectiveness);
                }

                // Render shadow
                var shadowRay = new Ray(intersection.Point, towardsLight);
                Intersection shadowIntersection;
                if (TryCalculateIntersection(shadowRay, scene, intersection.ObjectHit, out shadowIntersection) && shadowIntersection.Distance < lightDistance)
                {
                    var transparency = shadowIntersection.ObjectHit.Material.Transparency;
                    var lightPassThrough = Util.Lerp(.25f, 1.0f, transparency);
                    lightContribution = Color.Lerp(lightContribution, Color.Zero, 1 - lightPassThrough);
                }

                color += lightContribution;
            }

            if (depth < ReflectionDepth)
            {
                // Reflection ray
                var objectReflectivity = intersection.ObjectHit.Material.Reflectivity;
                if (objectReflectivity > 0.0f)
                {
                    var reflectionRay = GetReflectionRay(intersection.Point, intersection.Normal, intersection.ImpactDirection);
                    Intersection reflectionIntersection;
                    if (TryCalculateIntersection(reflectionRay, scene, intersection.ObjectHit, out reflectionIntersection))
                    {
                        color = Color.Lerp(color, CalculateRecursiveColor(reflectionIntersection, scene, depth + 1), objectReflectivity);
                    }
                }

                // Refraction ray
                var objectRefractivity = intersection.ObjectHit.Material.Refractivity;
                if (objectRefractivity > 0.0f)
                {
                    var refractionRay = GetRefractionRay(intersection.Point, intersection.Normal, intersection.ImpactDirection, objectRefractivity);
                    Intersection refractionIntersection;
                    if (TryCalculateIntersection(refractionRay, scene, intersection.ObjectHit, out refractionIntersection))
                    {
                        var refractedColor = CalculateRecursiveColor(refractionIntersection, scene, depth + 1);
                        color = Color.Lerp(color, refractedColor, 1 - (intersection.ObjectHit.Material.Opacity));
                    }
                }
            }

            color = color.Limited;
            return color;
        }

        /// <summary>
        /// Determines whether a given ray intersects with any scene objects (other than excludedObject)
        /// </summary>
        /// <param name="ray">The ray to test</param>
        /// <param name="scene">The scene to test</param>
        /// <param name="excludedObject">An object that is not tested for intersections</param>
        /// <param name="intersection">If the intersection test succeeds, contains the closest intersection</param>
        /// <returns>A value indicating whether or not any scene object intersected with the ray</returns>
        private bool TryCalculateIntersection(Ray ray, Scene scene, DrawableSceneObject excludedObject, out Intersection intersection)
        {
            var closestDistance = float.PositiveInfinity;
            var closestIntersection = new Intersection();
            foreach (var sceneObject in scene.DrawableObjects)
            {
                Intersection i;
                if (sceneObject != excludedObject && sceneObject.TryCalculateIntersection(ray, out i))
                {
                    if (i.Distance < closestDistance)
                    {

                        closestDistance = i.Distance;
                        closestIntersection = i;
                    }
                }
            }

            if (closestDistance == float.PositiveInfinity)
            {
                intersection = new Intersection();
                return false;
            }
            else
            {
                intersection = closestIntersection;
                return true;
            }
        }
    }

    public delegate void LineFinishedHandler(int rowNumber, Color[] lineColors);
}
