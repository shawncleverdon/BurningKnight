using System;
using BurningKnight.entity;
using BurningKnight.entity.level.levels;
using Lens.entity;
using Lens.util.file;

namespace BurningKnight.save {
	public class LevelSave {
		private static int I;

		public static void Save(Area area, FileWriter Writer) {
			var all = area.Tags[Tags.LevelSave];
			Writer.WriteInt32(all.Count);

			foreach (SaveableEntity Entity in all) {
				Writer.WriteString(Entity.GetType().FullName.Replace("BurningKnight.", ""));
				Entity.Save(Writer);
			}
		}

		public static void Load(Area area, FileReader Reader) {
			var Count = Reader.ReadInt32();

			for (var I = 0; I < Count; I++) {
				var entity = (SaveableEntity) Activator.CreateInstance(Type.GetType($"BurningKnight.{Reader.ReadString()}", true, false));

				area.Add(entity, false);
				entity.AddComponents();
				entity.Load(Reader);
				entity.PostInit();
			}
		}

		public static void Generate(Area area) {
			try {
				var level = new CastleLevel();
				area.Add(level);
				
				level.Generate(area, I);
				
				// todo: generate level
				I = 0;
			} catch (Exception e) {
				Console.WriteLine(e);
				Generate(area);
				I++;
			}
		}
	}
}