using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

using SCHEMA2NODE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Node>;

namespace SharpGLTF.Scenes
{
    partial class MeshContent : SCHEMA2NODE
    {
        void SCHEMA2NODE.ApplyTo(Node dstNode, Schema2SceneBuilder context)
        {
            // we try to assign our mesh to the target node.
            // but if the target node already has a mesh, we need to create
            // a child node that will contain our mesh.

            if (dstNode.Mesh != null) dstNode = dstNode.CreateNode();
            dstNode.Mesh = context.GetMesh(_Mesh);
        }
    }

    partial class CameraContent : SCHEMA2NODE
    {
        void SCHEMA2NODE.ApplyTo(Node dstNode, Schema2SceneBuilder context)
        {
            if (_Camera is CameraBuilder.Orthographic ortho)
            {
                if (dstNode.Camera != null) dstNode = dstNode.CreateNode();
                dstNode.WithOrthographicCamera(ortho.XMag, ortho.YMag, ortho.ZNear, ortho.ZFar);
            }

            if (_Camera is CameraBuilder.Perspective persp)
            {
                if (dstNode.Camera != null) dstNode = dstNode.CreateNode();
                dstNode.WithPerspectiveCamera(persp.AspectRatio, persp.VerticalFOV, persp.ZNear, persp.ZFar);
            }
        }
    }

    partial class LightContent : SCHEMA2NODE
    {
        void SCHEMA2NODE.ApplyTo(Node dstNode, Schema2SceneBuilder context)
        {
            if (_Light is LightBuilder.Directional directional)
            {
                if (dstNode.Camera != null) dstNode = dstNode.CreateNode();
                dstNode.PunctualLight = dstNode.LogicalParent.CreatePunctualLight(PunctualLightType.Directional);
                dstNode.PunctualLight.Color = directional.Color;
                dstNode.PunctualLight.Intensity = directional.Intensity;
            }

            if (_Light is LightBuilder.Point point)
            {
                if (dstNode.Camera != null) dstNode = dstNode.CreateNode();
                dstNode.PunctualLight = dstNode.LogicalParent.CreatePunctualLight(PunctualLightType.Point);
                dstNode.PunctualLight.Color = point.Color;
                dstNode.PunctualLight.Intensity = point.Intensity;
                dstNode.PunctualLight.Range = point.Range;
            }

            if (_Light is LightBuilder.Spot spot)
            {
                if (dstNode.Camera != null) dstNode = dstNode.CreateNode();
                dstNode.PunctualLight = dstNode.LogicalParent.CreatePunctualLight(PunctualLightType.Spot);
                dstNode.PunctualLight.Color = spot.Color;
                dstNode.PunctualLight.Intensity = spot.Intensity;
                dstNode.PunctualLight.Range = spot.Range;
                dstNode.PunctualLight.SetSpotCone(spot.InnerConeAngle, spot.OuterConeAngle);
            }
        }
    }
}
