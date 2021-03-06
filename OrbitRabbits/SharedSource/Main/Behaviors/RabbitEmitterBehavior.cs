﻿#region File Description
//-----------------------------------------------------------------------------
// OrbitRabbits
//
// Quickstarter for Wave University Tour 2014.
// Author: Wave Engine Team
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using OrbitRabbits.Commons;
using OrbitRabbits.Components;
using OrbitRabbits.Entities.Particles;
using OrbitRabbits.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Components.Particles;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services;
using WaveEngine.Materials;
#endregion

namespace OrbitRabbits.Entities.Behaviors
{
    [DataContract(Namespace = "OrbitRabbits.Entities.Behaviors")]
    public class RabbitEmitterBehavior : Behavior
    {
        private Vector2 initialRabbitPosition;
        private List<Rabbit> deadRabbits;
        private Rabbit lastRabbit;
        private TimeSpan time;

        public List<Rabbit> Rabbits;
        public event EventHandler<int> ScoreChanged;

        private Entity explosion 
        {
            get
            {
                return this.Owner.FindChild("explosion");
            }
        }

        private ParticleSystem2D explosionParticles
        {
            get
            {
                Entity explosionEntity = this.explosion;
                if(explosionEntity == null)
                {
                    return null;
                }

                return explosionEntity.FindComponent<ParticleSystem2D>();
            }
        }

        private Transform2D explosionTransform
        {
            get
            {
                Entity explosionEntity = this.explosion;
                if (explosionEntity== null)
                {
                    return null;
                }

                return explosionEntity.FindComponent<Transform2D>();
            }
        }
        
        /// <summary>
        /// Sets default values
        /// </summary>
        protected override void DefaultValues()
        {
            base.DefaultValues();

            this.initialRabbitPosition = new Vector2(375, 460);
            this.Rabbits = new List<Rabbit>();
            this.deadRabbits = new List<Rabbit>();
        }

        protected override void Initialize()
        {
            base.Initialize();

            if(this.explosionParticles != null)
            {
                this.explosionParticles.LinearColorEnabled = true;
                this.explosionParticles.InterpolationColors = new List<WaveEngine.Common.Graphics.Color>() { Color.White, Color.Black };
            }
        }

        /// <summary>
        /// Allows this instance to execute custom logic during its <c>Update</c>.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <remarks>
        /// This method will not be executed if the <see cref="T:WaveEngine.Framework.Component" />, or the <see cref="T:WaveEngine.Framework.Entity" />
        /// owning it are not <c>Active</c>.
        /// </remarks>
        protected override void Update(TimeSpan gameTime)
        {
            bool updateScore = false;

            this.time += gameTime;

            if ((this.lastRabbit != null && (this.lastRabbit.State == Rabbit.RabbitState.dying ||
                this.lastRabbit.State == Rabbit.RabbitState.dead)) ||
                this.time > TimeSpan.FromSeconds(5))
            {
                this.AddRabbit();
                this.time = TimeSpan.Zero;
            }

            // Clear rabbit deads
            foreach (Rabbit rabbit in this.Rabbits)
            {
                if (rabbit.State == Rabbit.RabbitState.dead)
                {
                    this.deadRabbits.Add(rabbit);
                }
            }

            if (this.deadRabbits.Count > 0)
            {
                foreach (Rabbit deadRabbit in deadRabbits)
                {
                    this.CreateExplosion(deadRabbit);
                    this.Owner.RemoveChild(deadRabbit.Name);
                    this.Rabbits.Remove(deadRabbit);
                }
                this.deadRabbits.Clear();

                updateScore = true;
            }

            // Check Collision
            for (int i = 0; i < this.Rabbits.Count - 1; i++)
            {
                Rabbit rabbit1 = this.Rabbits[i];
                if (rabbit1.State != Rabbit.RabbitState.afloat) { continue; }

                for (int j = i + 1; j < this.Rabbits.Count; j++)
                {
                    Rabbit rabbit2 = this.Rabbits[j];
                    if (rabbit2.State != Rabbit.RabbitState.afloat) { continue; }

                    bool collision = rabbit1.Collision(rabbit2);
                    if (collision)
                    {
                        rabbit1.State = Rabbit.RabbitState.dead;
                        rabbit2.State = Rabbit.RabbitState.dead;
                        updateScore = true;

                        this.CreateExplosion(rabbit1, rabbit2);
                    }
                }
            }

            // Update Scores
            if (updateScore)
            {
                this.NotifyNewScore();
            }
        }

        /// <summary>
        /// Adds the rabbit.
        /// </summary>
        public void AddRabbit()
        {
            Rabbit newRabbit = new Rabbit(this.initialRabbitPosition, 0.75f, this.Assets);
            this.Owner.AddChild(newRabbit.Entity);
            this.Rabbits.Add(newRabbit);

            this.lastRabbit = newRabbit;

            this.NotifyNewScore();
        }

        /// <summary>
        /// Notifies the new score.
        /// </summary>
        private void NotifyNewScore()
        {
            if (this.ScoreChanged != null)
            {
                int rabbitAliveCount = (from rabbit in this.Rabbits
                                        where rabbit.State == Rabbit.RabbitState.afloat
                                        select rabbit).Count();
                this.ScoreChanged(this, rabbitAliveCount);
            }
        }

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        public void ApplyImpulseToLast()
        {
            if (this.lastRabbit != null)
            {
                this.time = TimeSpan.Zero;
                this.lastRabbit.ApplyImpulse();

                SoundsManager.Instance.PlaySound(SoundsManager.SOUNDS.Impulse);
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            foreach (Rabbit rabbit in this.Rabbits)
            {
                this.Owner.RemoveChild(rabbit.Name);
            }
            this.Rabbits.Clear();
        }

        /// <summary>
        /// Creates the explosion.
        /// </summary>
        /// <param name="rabbit1">The rabbit1.</param>
        /// <param name="rabbit2">The rabbit2.</param>
        private void CreateExplosion(Rabbit rabbit1, Rabbit rabbit2)
        {
            var explosionSystem = this.explosionParticles;
            var explosionTransform = this.explosionTransform;
            if (explosionSystem == null || explosionTransform == null)
            {
                return;
            }

            Vector2 rabbit1Position = new Vector2(rabbit1.Transform2D.X, rabbit1.Transform2D.Y);
            Vector2 rabbit2Position = new Vector2(rabbit2.Transform2D.X, rabbit2.Transform2D.Y);

            Vector2 distance = rabbit2Position - rabbit1Position;
            float length = distance.Length();
            distance.Normalize();


            Vector2 explosionPosition = rabbit1Position + distance * length / 2;
            explosionTransform.X = explosionPosition.X;
            explosionTransform.Y = explosionPosition.Y;

            explosionSystem.Emit = true;
            WaveServices.TimerFactory.CreateTimer("explosionTimer", TimeSpan.FromSeconds(0.1), () =>
            {
                explosionSystem.Emit = false;
            }, false);

            SoundsManager.Instance.PlaySound(SoundsManager.SOUNDS.Explosion);
        }

        /// <summary>
        /// Creates the explosion.
        /// </summary>
        /// <param name="rabbit1">The rabbit1.</param>
        private void CreateExplosion(Rabbit rabbit1)
        {
            var explosionSystem = this.explosionParticles;
            var explosionTransform = this.explosionTransform;
            if (explosionSystem == null || explosionTransform == null)
            {
                return;
            }

            Vector2 explosionPosition = new Vector2(rabbit1.Transform2D.X, rabbit1.Transform2D.Y);
            explosionTransform.X = explosionPosition.X;
            explosionTransform.Y = explosionPosition.Y;

            explosionSystem.Emit = true;
            WaveServices.TimerFactory.CreateTimer("explosionTimer", TimeSpan.FromSeconds(0.1), () =>
            {
                explosionSystem.Emit = false;
            }, false);

            SoundsManager.Instance.PlaySound(SoundsManager.SOUNDS.Explosion);
        }


    }
}
