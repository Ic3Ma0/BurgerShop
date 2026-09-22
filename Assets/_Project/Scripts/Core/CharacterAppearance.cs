using UnityEngine;

namespace BurgerShop.Core
{
    // Visual identity only. Ticket selection never consumes the simulation's random stream.
    public static class CharacterAppearance
    {
        public const int StaffCount = 3;
        public const int CustomerCount = 12;
        public static readonly Color Cream = Hex(0xFFF0D4);
        public static readonly Color Ink = Hex(0x263341);
        public static readonly Color Red = Hex(0xE74E38);
        public static readonly Color[] Skin = { Hex(0xEAAF82), Hex(0xB97750), Hex(0x784630), Hex(0xF5C8A0) };
        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        public static int CustomerIndex(int ticket) => (int)((uint)Mathf.Max(0, ticket - 1) % CustomerCount);
        public static int SkinIndex(int ticket) => (int)(((long)Mathf.Max(1, ticket) - 1) / CustomerCount + CustomerIndex(ticket)) % Skin.Length;
        public static StaffLook Staff(int slot) => StaffLooks[Mathf.Clamp(slot, 0, StaffCount - 1)];
        public static CustomerLook Customer(int ticket) => CustomerLooks[CustomerIndex(ticket)];

        static readonly StaffLook[] StaffLooks = {
            new StaffLook('A', new Color(.13f,.58f,.64f), 1f, true),
            new StaffLook('B', new Color(.86f,.42f,.16f), .88f, false),
            new StaffLook('C', new Color(.42f,.28f,.62f), 1.14f, true)
        };
        // Deliberate outfits, rather than arbitrary combinations of incompatible pieces.
        static readonly CustomerLook[] CustomerLooks = {
            new CustomerLook("Campus", 0xE9AC3E, 0x385677, 0x392A29, 0, 0),
            new CustomerLook("Weekend", 0xD77464, 0x30495D, 0x5B3327, 1, 1),
            new CustomerLook("Commuter", 0x78B6BA, 0x263B50, 0x282B30, 2, 2),
            new CustomerLook("Skater", 0x548998, 0xCCB894, 0x482F27, 3, 3),
            new CustomerLook("Reader", 0x9C8CB8, 0x475161, 0xD3CDC1, 4, 2),
            new CustomerLook("Music", 0xDD8550, 0x3B485D, 0x302B2A, 5, 4),
            new CustomerLook("Garden", 0x88A95D, 0xEDD1A2, 0x75452D, 6, 1),
            new CustomerLook("Denim", 0x5989BC, 0x394252, 0xDEA74E, 7, 0),
            new CustomerLook("Cozy", 0xD8829A, 0x51465D, 0x3A292D, 4, 3),
            new CustomerLook("Sailor", 0xE9E4D5, 0x2F677C, 0x9A5C34, 1, 0),
            new CustomerLook("Artist", 0x51A397, 0x4E5266, 0x3D302D, 6, 2),
            new CustomerLook("Sport", 0xDF6954, 0xE5D6B6, 0x292D35, 5, 0)
        };
        public readonly struct StaffLook
        {
            public readonly char Letter;
            public readonly Color Uniform;
            public readonly float Height;
            public readonly bool HasHat;
            public StaffLook(char letter, Color uniform, float height, bool hat)
            { Letter = letter; Uniform = uniform; Height = height; HasHat = hat; }
        }
        public readonly struct CustomerLook
        {
            public readonly string Name;
            public readonly Color Shirt, Trousers, Hair;
            public readonly int HairStyle, Accessory;
            public CustomerLook(string name, int shirt, int trousers, int hair, int style, int accessory)
            { Name = name; Shirt = Hex(shirt); Trousers = Hex(trousers); Hair = Hex(hair); HairStyle = style; Accessory = accessory; }
        }
    }
}
