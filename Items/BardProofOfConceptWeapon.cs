using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NewFrontiersMod.Items {
    public abstract class BardWeapon : ModItem {
        bool prevClick;
        public override bool CloneNewInstances => true;
        public int noteIndex;
        public int noteTime;
        public bool restart = false;
        public const int precision = 5;
        public Item MusicSheet { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if the music sheet was successfully set</returns>
        public bool SetMusicSheet(Item item) {
            if (item is null) {
                MusicSheet = new Item();
                return true;
            }
            if (item.IsAir || item.modItem is BardSong) {
                MusicSheet = item;
                return true;
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            for (int i = tooltips.Count-1; i >= 0; i--) {
                if (tooltips[i].text.Equals("SongName")) {
                    string name = "None";
                    if (!(MusicSheet?.IsAir??true)) {
                        name = MusicSheet.Name;
                    }
                    tooltips[i].text = "Song: " + name;
                }
            }
        }
        public override bool CanRightClick() {
            if (Main.mouseRightRelease) {
                Item current = MusicSheet;
                if (SetMusicSheet(Main.mouseItem)) {
                    Main.mouseItem = current??new Item();
                    Main.PlaySound(SoundID.Grab);
                }
            }
            return false;//item is consumed if true is returned
        }
        public override void Load(TagCompound tag) {
            if (tag.ContainsKey("musicSheet")) MusicSheet = tag.Get<Item>("musicSheet");
        }
        public override TagCompound Save() {
            return new TagCompound() {
                { "musicSheet", MusicSheet }
            };
        }
        public override void HoldItem(Player player) {
            if (noteTime > -precision) {
                noteTime--;
                bool shouldPlay = noteTime < precision;
                for (int i = 0; i < 60; i++) {
                    Dust.NewDustPerfect(player.Center + new Vector2(precision * 2, 0).RotatedBy(0.10471975511965977461542144610932 * i), DustID.DungeonWater, Vector2.Zero, Scale:0.5f).noGravity = true;
                    Dust.NewDustPerfect(player.Center + new Vector2(noteTime + precision, 0).RotatedBy(0.10471975511965977461542144610932 * i), shouldPlay ? DustID.Fire : DustID.DungeonWater, Vector2.Zero, Scale:0.5f).noGravity = true;
                }
            } else if(player.itemAnimation == 0){
                restart = true;
            }
        }
        
        public Note GetNextNote(int index) {
            if (MusicSheet?.modItem is BardSong song && !MusicSheet.IsAir) {
                return song.GetNextNote(index);
            }
            return Note.Default;
        }
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            bool useRestart = false;
            if (restart) {
                useRestart = restart && Terraria.GameInput.PlayerInput.Triggers.Current.MouseLeft;
                noteIndex = 0;
                prevClick = false;
            }
            if(noteTime > -precision || useRestart){
				player.itemAnimation = item.useAnimation-1;
                if (player.controlUseItem) {
                    if (!prevClick || useRestart) {
                        if (noteTime < precision) {
                            restart = false;
                            Note note = GetNextNote(noteIndex++);
                            noteTime = note.delay;
                            SpawnProjectile(player, note, position, new Vector2(speedX, speedY), (int)(damage * note.damageMult), knockBack);
                        } else {
                            noteTime = -precision;
                        }
                    }
                    prevClick = true;
                } else {
                    prevClick = false;
                }
			}
            return false;
        }
        public abstract void SpawnProjectile(Player player, Note note, Vector2 position, Vector2 velocity, int damage, float knockBack);
    }
    public abstract class BardSong : ModItem {
        public abstract Note GetNextNote(int index);
    }
    public struct Note {
        public static Note Default => new Note(0, 65);
        public int type;
        public int extraCount;
        public BardAttackMode mode;
        public int delay;
        public float damageMult;
        public float pitch;
        public Note(int type, int delay, float pitch = 1f, float damageMult = 1f, int extraCount = 0, BardAttackMode mode = BardAttackMode.NORMAL) {
            this.type = type;
            this.delay = delay;
            this.extraCount = extraCount;
            this.mode = mode;
            this.damageMult = damageMult;
            this.pitch = pitch;
        }
    }
    public enum BardAttackMode {
        NORMAL = 0
    }
    public class BardProofOfConceptWeapon : BardWeapon {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("SongName");
        }
        public override void SetDefaults() {
            item.damage = 17;
            item.useStyle = 5;
            item.useTime = 2;
            item.useAnimation = 17;
            item.shoot = 1;
            item.shootSpeed = 6;
        }
        public override void SpawnProjectile(Player player, Note note, Vector2 position, Vector2 velocity, int damage, float knockBack) {
            int projectileType;
            switch (note.type) {
                default:
                projectileType = ProjectileID.AmethystBolt;
                break;
                case 1:
                projectileType = ProjectileID.EmeraldBolt;
                break;
                case 2:
                projectileType = ProjectileID.AmberBolt;
                break;
            }
            switch (note.mode) {
                case BardAttackMode.NORMAL: {
                    Vector2 perp = velocity.RotatedBy(MathHelper.PiOver2) * 2;
                    bool halfOffset = (note.extraCount & 1) != 0;
                    for (int i = 0; i <= note.extraCount; i++) {
                        float offsetMult = ((i + 1) >> 1) * ((i & 1) == 0 ? 1 : -1) + (halfOffset ? 0 : 0.5f);
                        Projectile.NewProjectile(position + perp * offsetMult, velocity, projectileType, damage, knockBack, player.whoAmI);
                    }
                    Main.PlaySound(SoundID.Item, (int)position.X, (int)position.Y, 26, 1, note.pitch);
                    break;
                }
            }
        }
    }
    public class BardProofOfConceptSong : BardSong {
        public override Note GetNextNote(int index) {
            return new Note(index % 3, 45, extraCount:index);
        }
    }
    public class BardExampleSong : BardSong {
        public override Note GetNextNote(int index) {
            switch (index % 11) {
                case 0:
                case 3:
                case 6:
                return new Note(0, 25, 0.5f, damageMult:(index / 11)+1);
                case 1:
                case 4:
                case 7:
                return new Note(1, 25, 0.25f, damageMult:(index / 11)+1);
                case 2:
                case 5:
                case 10:
                return new Note(2, 60, 0.1f, damageMult:(index / 11)+1);
                case 8:
                return new Note(2, 40, 0.1f, damageMult:(index / 11)+1);
                case 9:
                return new Note(1, 40, 0.25f, damageMult:(index / 11)+1);
            }
            return Note.Default;
        }
    }
}
