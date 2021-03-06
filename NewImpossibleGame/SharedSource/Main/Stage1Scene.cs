#region Using Statements
using System;
using WaveEngine.Common;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.Cameras;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Components.Graphics3D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Resources;
using WaveEngine.Framework.Services;
#endregion

namespace NewImpossibleGame
{
    public class Stage1Scene : Scene
    {
        /// <summary>
        /// Creates the scene.
        /// </summary>
        protected override void CreateScene()
        {
            //// this.RenderManager.DebugLines = true;
            this.Load(WaveContent.Scenes.Stage1);
        }
    }
}
