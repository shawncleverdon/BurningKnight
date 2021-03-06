using System;
using BurningKnight.entity.component;
using BurningKnight.entity.door;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.util;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.util;
using Lens.util.math;
using Lens.util.tween;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.mob.castle {
	public class Gunner : Mob {
		protected override void SetStats() {
			base.SetStats();
			
			AddAnimation("gunner");
			SetMaxHp(5);
			
			Become<IdleState>();
			
			var body = new RectBodyComponent(1, 15, 15, 1);
			AddComponent(body);

			body.Body.LinearDamping = 6;
			
			AddComponent(new SensorBodyComponent(2, 2, 12, 12));

			moveId = Rnd.Int(3);
			GetComponent<AudioEmitterComponent>().PitchMod = -0.3f;
		}

		#region Gunner States
		public class IdleState : SmartState<Gunner> {
			private float delay;

			public override void Init() {
				base.Init();
				delay = Rnd.Float(0.4f, 1f);
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (T >= delay) {
					Become<RunState>();
				}
			}
		}

		private int moveId;
		
		public class RunState : SmartState<Gunner> {
			private const float Accuracy = 0.2f;
			
			private Vector2 velocity;
			private float timer;
			private float lastBullet;
			private float angle;
			private bool fire;
			private float start;
			private bool saw;
			
			public override void Init() {
				base.Init();

				fire = Self.Target != null && Self.moveId % 2 == 0;
				angle = !fire ? Rnd.AnglePI() : Self.AngleTo(Self.Target);
				timer = fire ? 0.9f : Rnd.Float(0.8f, 2f);
				start = Rnd.Float(0f, 10f);
				
				var a = angle + Rnd.Float(-Accuracy, Accuracy);
				var force = fire ? 60 : 120;
				
				velocity.X = (float) Math.Cos(a) * force;
				velocity.Y = (float) Math.Sin(a) * force;

				Self.GetComponent<RectBodyComponent>().Velocity = velocity;
				Self.moveId++;
			}

			public override void Destroy() {
				base.Destroy();
				Self.GetComponent<RectBodyComponent>().Velocity = Vector2.Zero;
			}

			public override void Update(float dt) {
				base.Update(dt);
				
				if (timer <= T) {
					Become<IdleState>();
					return;
				}

				var v = velocity * Math.Min(1, timer - T * 0.4f);
				Self.GetComponent<RectBodyComponent>().Velocity = v;

				if (!fire) {
					return;
				}

				lastBullet -= dt;
				
				if (lastBullet <= 0) {
					lastBullet = 0.3f;

					if (!saw && !Self.CanSeeTarget()) {
						return;
					}

					saw = true;
					
					var an = angle + Rnd.Float(-Accuracy, Accuracy) + Math.Cos(T * 6f + start) * (float) Math.PI * 0.1f;
					var a = Self.GetComponent<MobAnimationComponent>();
					
					Tween.To(1.8f, a.Scale.X, x => a.Scale.X = x, 0.1f);
					Tween.To(0.2f, a.Scale.Y, x => a.Scale.Y = x, 0.1f).OnEnd = () => {

						Tween.To(1, a.Scale.X, x => a.Scale.X = x, 0.2f);
						Tween.To(1, a.Scale.Y, x => a.Scale.Y = x, 0.2f);
						
						Self.GetComponent<AudioEmitterComponent>().EmitRandomized("mob_fire", sz: 0.2f);
						
						var projectile = Projectile.Make(Self, "circle", an, 7f);

						projectile.AddLight(32f, Projectile.RedLight);
						projectile.Center += MathUtils.CreateVector(angle, 8);

						AnimationUtil.Poof(projectile.Center);
					};
				}
			}
		}
		#endregion

		public override bool HandleEvent(Event e) {
			if (e is CollisionStartedEvent ev) {
				if (ev.Entity is Door) {
					var s = GetComponent<StateComponent>().StateInstance;

					if (s is RunState) {
						Become<IdleState>();
					}
				}
			}
			
			return base.HandleEvent(e);
		}

		protected override string GetDeadSfx() {
			return "mob_gunner_death";
		}

		protected override string GetHurtSfx() {
			return "mob_gunner_hurt";
		}
	}
}