using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	public class ColonyInfo : TraitInfo, Requires<TurretedInfo>
	{
		public readonly string LostSound = "sounds|POWERDOWN.SDF";

		[ActorReference(typeof(DefeatedColonyInfo))]
		public readonly string SpawnsActor = "UnclaimedColony";

		public readonly string NewOwner = "Neutral";

		public readonly CVec Offset = new CVec(0, 0);

		public override object Create(ActorInitializer init)
		{
			return new Colony(this, init.Self);
		}
	}

	public class Colony : INotifyKilled
	{
		readonly ColonyInfo info;
		readonly IEnumerable<Turreted> turreted;

		public Colony(ColonyInfo info, Actor self)
		{
			this.info = info;
			turreted = self.TraitsImplementing<Turreted>();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			Game.Sound.Play(SoundType.World, info.LostSound, self.CenterPosition);

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + info.Offset),
				new CenterPositionInit(self.CenterPosition),
				new OwnerInit(info.NewOwner),
			};

			foreach (var t in turreted)
				td.Add(new TurretFacingInit(t.Info, t.LocalOrientation.Yaw));

			self.World.AddFrameEndTask(w => w.CreateActor(info.SpawnsActor, td));
		}

		public void CancelProductions(Actor self)
		{
			foreach (var productionQueue in self.TraitsImplementing<ProductionQueue>())
			{
				while (true)
				{
					var producing = productionQueue.AllQueued().ToArray();

					if (!producing.Any())
						break;

					foreach (var productionItem in producing)
						productionQueue.EndProduction(productionItem);
				}
			}
		}
	}
}
