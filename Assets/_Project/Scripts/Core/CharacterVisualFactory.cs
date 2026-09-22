using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Core
{
    /// <summary>Restaurant cast geometry. No economy, navigation or persistence dependencies.</summary>
    public static class CharacterVisualFactory
    {
        public static void Player(Transform root)
        {
            var b = new Builder(root, -1f, CharacterAppearance.Cream, CharacterAppearance.Skin[1], CharacterAppearance.Hex(0x352B29), CharacterAppearance.Ink);
            b.Body("ChefJacket", .72f);
            b.Hair(-1);
            b.Apron(b.Red, true);
            b.P("ChefHat", PrimitiveType.Cylinder, 0,1.96f,0, .67f,.16f,.56f,b.White);
            for (int i = -1; i <= 1; i++) b.S("HatPuff", i*.21f,2.16f,0, .39f,.38f,.55f,b.White);
            b.P("HatBand",PrimitiveType.Cylinder,0,1.94f,0,.69f,.035f,.58f,b.Red);
            b.Scarf(b.Red);
            foreach (float x in new[] { -.14f, .14f })
                for (int i=0;i<2;i++) b.S("ChefButton",x,1.18f-i*.12f,.247f,.05f,.05f,.035f,b.Ink);
            b.Finish();
        }

        public static void Staff(Transform root, int slot)
        {
            slot = Mathf.Clamp(slot, 0, CharacterAppearance.StaffCount - 1);
            var look = CharacterAppearance.Staff(slot);
            // The existing gameplay root is scaled per employee. Keep the shoes on the floor.
            var b = new Builder(root,-1.05f/look.Height,look.Uniform,CharacterAppearance.Skin[slot % 3],CharacterAppearance.Hex(slot==1?0x6B3829:0x332C2A),CharacterAppearance.Ink);
            b.Body("Uniform",slot==1?.64f:.7f);
            b.Hair(slot==1?6:slot==2?5:-1);
            b.Apron(b.White,false);
            b.P("NameBadge",PrimitiveType.Cube,-.14f,1.19f,.272f,.19f,.10f,.025f,b.Ink);
            b.P("BadgeStripe",PrimitiveType.Cube,-.14f,1.19f,.287f,.11f,.025f,.015f,b.White);
            if(slot==0)
            {
                b.P("Hat",PrimitiveType.Cylinder,0,1.91f,-.02f,.7f,.075f,.56f,b.Red);
                b.S("CapBrim",0,1.85f,.28f,.66f,.07f,.44f,b.Red);
                b.P("CapBadge",PrimitiveType.Cube,0,1.96f,.245f,.16f,.075f,.035f,b.White);
            }
            else if(slot==1)
            {
                b.S("Bow",-.25f,1.94f,-.08f,.3f,.14f,.15f,b.White);
                b.Scarf(b.White);
            }
            else
            {
                b.P("Hat",PrimitiveType.Cylinder,0,1.88f,0,.67f,.035f,.58f,b.White);
                b.S("Visor",0,1.84f,.30f,.70f,.065f,.34f,b.White);
                b.Glasses();
                b.Scarf(b.Red);
            }
            b.Finish();
        }

        public static void Customer(Transform root,int ticket,bool bigEater=false,bool calling=false)
        {
            if(bigEater)
            {
                BigEater(root,ticket);
                return;
            }
            var look=CharacterAppearance.Customer(ticket);
            int index=CharacterAppearance.CustomerIndex(ticket);
            var b=new Builder(root,0,look.Shirt,CharacterAppearance.Skin[CharacterAppearance.SkinIndex(ticket)],look.Hair,look.Trousers);
            b.Body("Body",index==4?.62f:index==8?.79f:.69f);
            b.Hair(look.HairStyle);
            if(index==0 || index==7)
            {
                b.P("JacketInset",PrimitiveType.Cube,0,1.06f,.234f,.25f,.48f,.035f,b.White);
                foreach(float side in new[]{-1f,1f})b.P("JacketPocket",PrimitiveType.Cube,side*.22f,1.11f,.23f,.12f,.11f,.035f,b.Accent);
            }
            else if(index==9)
            {
                for(int i=0;i<3;i++)b.P("SailorStripe",PrimitiveType.Cube,0,.91f+i*.12f,.25f,.51f,.035f,.02f,b.Accent);
            }
            else if(index==2)
            {
                b.P("ShirtPlacket",PrimitiveType.Cube,0,1.05f,.254f,.03f,.47f,.025f,b.White);
                b.P("Tie",PrimitiveType.Cube,0,1.14f,.28f,.095f,.26f,.03f,b.Ink);
            }
            else if(index==8) b.Scarf(b.White);
            else if(index==11)
            {
                foreach(float x in new[]{-.1f,0f,.1f})b.P("SportStripe",PrimitiveType.Cube,x,1.12f,.258f,.045f,.19f,.025f,b.White);
            }
            else if(index==5) b.S("SweatshirtPrint",0,1.1f,.258f,.22f,.20f,.025f,b.Accent);
            else if(index==6) b.P("PleatedSkirt",PrimitiveType.Capsule,0,.66f,0,.76f,.17f,.48f,b.Accent);
            else if(index==4) b.P("SweaterHem",PrimitiveType.Capsule,0,.77f,0,.69f,.07f,.48f,b.Accent);

            switch(look.Accessory)
            {
                case 1:
                    var strap=b.P("SatchelStrap",PrimitiveType.Cube,0,1.03f,.265f,.065f,.67f,.035f,b.Accent);
                    strap.localRotation=Quaternion.Euler(0,0,-32);
                    b.S("Satchel",.35f,.72f,.08f,.28f,.34f,.25f,b.Accent);
                    b.P("SatchelClasp",PrimitiveType.Cube,.35f,.76f,.214f,.06f,.07f,.02f,b.White);
                    break;
                case 2: b.Glasses(); break;
                case 3:
                    b.S("Backpack",0,1.04f,-.30f,.48f,.55f,.27f,b.Accent);
                    foreach(float x in new[]{-.22f,.22f})b.P("BackpackStrap",PrimitiveType.Cube,x,1.17f,.23f,.075f,.32f,.03f,b.Accent);
                    break;
                case 4:
                    foreach(float x in new[]{-.35f,.35f})b.S("HeadphoneCup",x,1.68f,0,.16f,.27f,.23f,b.White);
                    b.S("HeadphoneBand",0,1.96f,0,.65f,.075f,.18f,b.White);
                    break;
            }
            if(calling)
            {
                b.P("Phone",PrimitiveType.Cube,.39f,1.55f,.19f,.10f,.29f,.15f,b.Ink);
                root.GetComponent<HumanoidVisual>().SetPhonePose();
            }
            b.Finish();
        }

        static void BigEater(Transform root,int ticket)
        {
            var b=new Builder(root,0,CharacterAppearance.Hex(0xEAB24F),
                CharacterAppearance.Skin[CharacterAppearance.SkinIndex(ticket)],
                CharacterAppearance.Hex(0x65412E),CharacterAppearance.Hex(0x3A5268));
            b.Body("Body",1.0f);
            root.Find("Body").localScale=new Vector3(1.0f,.36f,.66f);
            root.Find("Head").localScale=new Vector3(.71f,.65f,.59f);
            b.Hair(2);
            b.RoundBelly();
            foreach(float side in new[]{-1f,1f})
            {
                b.S("Cheek",side*.20f,1.52f,.17f,.17f,.15f,.075f,b.Skin);
                // Connect segments along the belly so the suspenders do not form stair steps.
                Vector3 previous=SuspenderPoint(side,.70f);
                for(int i=1;i<=7;i++)
                {
                    Vector3 next=SuspenderPoint(side,.70f+i*.08f);
                    Vector3 center=(previous+next)*.5f;
                    var strap=b.P("Suspender",PrimitiveType.Cube,center.x,center.y,center.z,.075f,(next-previous).magnitude+.012f,.025f,b.Accent);
                    strap.localRotation=Quaternion.FromToRotation(Vector3.up,next-previous);
                    previous=next;
                }
                b.P("SuspenderClip",PrimitiveType.Cube,side*.27f,.76f,.325f,.105f,.08f,.035f,b.White);
                var leg=root.Find(side<0?"LeftLeg":"RightLeg");
                leg.localPosition=new Vector3(side*.235f,leg.localPosition.y,leg.localPosition.z);
            }
            // A small burger print identifies the large-order guest without an employee badge.
            b.S("BurgerPrintTop",0,1.12f,.378f,.23f,.105f,.035f,b.White);
            b.P("BurgerPrintPatty",PrimitiveType.Cube,0,1.053f,.407f,.25f,.037f,.03f,b.Ink);
            b.P("BurgerPrintTomato",PrimitiveType.Cube,0,1.026f,.419f,.26f,.025f,.028f,b.Red);
            b.S("BurgerPrintBottom",0,.984f,.435f,.24f,.065f,.028f,b.White);
            b.Finish();
        }

        static Vector3 SuspenderPoint(float side,float y)
        {
            float belly=.05f+.38f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((y-.9f)/.36f,2)-Mathf.Pow(.27f/.51f,2)));
            return new Vector3(side*.27f,y,Mathf.Max(.28f,belly)+.023f);
        }

        sealed class Builder
        {
            readonly Transform root;
            readonly float feet;
            readonly CharacterVisualResources owner;
            readonly Material shirt,skin,hair,trousers;
            public readonly Material White,Ink,Red,Accent;
            public Material Skin => skin;
            public Builder(Transform root,float feet,Color shirt,Color skin,Color hair,Color trousers)
            {
                this.root=root;this.feet=feet;
                owner=root.gameObject.AddComponent<CharacterVisualResources>();
                this.shirt=Mat(shirt);this.skin=Mat(skin);this.hair=Mat(hair);this.trousers=Mat(trousers);
                White=Mat(CharacterAppearance.Cream);Ink=Mat(CharacterAppearance.Ink);Red=Mat(CharacterAppearance.Red);
                Accent=Mat(Color.Lerp(trousers,CharacterAppearance.Cream,.22f));
            }
            Material Mat(Color color)
            {
                var mat=RuntimeMaterials.Create(color);
                owner.Materials.Add(mat);return mat;
            }
            public Transform P(string name,PrimitiveType type,float x,float y,float z,float sx,float sy,float sz,Material mat)
            {
                return BagVisualFactory.Part(root,name,type,new Vector3(x,y+feet,z),new Vector3(sx,sy,sz),mat).transform;
            }
            public Transform S(string name,float x,float y,float z,float sx,float sy,float sz,Material mat) => P(name,PrimitiveType.Sphere,x,y,z,sx,sy,sz,mat);
            public void RoundBelly() => S("RoundBelly",0,.9f,.05f,1.02f,.72f,.76f,shirt);
            public void Body(string name,float width)
            {
                P(name,PrimitiveType.Capsule,0,1.0f,0,width,.34f,.48f,shirt);
                P("Waist",PrimitiveType.Capsule,0,.66f,0,width*.78f,.095f,.39f,trousers);
                S("Neck",0,1.35f,0,.25f,.24f,.24f,skin);
                S("Head",0,1.62f,0,.64f,.65f,.56f,skin);
                foreach(float side in new[]{-1f,1f})
                {
                    S("Ear",side*.30f,1.61f,0,.15f,.21f,.15f,skin);
                    S("Eye",side*.117f,1.65f,.265f,.045f,.064f,.026f,Ink);
                    P("Brow",PrimitiveType.Cube,side*.117f,1.73f,.259f,.075f,.025f,.026f,hair);
                }
                S("Nose",0,1.57f,.287f,.11f,.105f,.12f,skin);
                S("Smile",0,1.47f,.262f,.09f,.027f,.018f,Ink);
                HumanoidVisual.Add(root,feet,shirt,skin,trousers);
                // Moving limbs retain their original anchors; soles and cuffs move with them.
                foreach(string side in new[]{"Left","Right"})
                {
                    var leg=root.Find(side+"Leg");
                    BagVisualFactory.Part(leg,"Sole",PrimitiveType.Sphere,new Vector3(0,-.615f,.07f),new Vector3(.27f,.065f,.40f),White);
                    var arm=root.Find(side+"Arm");
                    arm.localPosition=new Vector3((side=="Left"?-1:1)*(width*.5f+.075f),arm.localPosition.y,0);
                    BagVisualFactory.Part(arm,"Cuff",PrimitiveType.Capsule,new Vector3(0,-.265f,0),new Vector3(.227f,.045f,.25f),White);
                }
            }
            public void Apron(Material color,bool chef)
            {
                P("Apron",PrimitiveType.Cube,0,.80f,.26f,.51f,.40f,.055f,color);
                if(!chef)P("ApronBib",PrimitiveType.Cube,0,1.08f,.255f,.39f,.28f,.045f,color);
                P("ApronBelt",PrimitiveType.Cube,0,.98f,.268f,.58f,.055f,.04f,chef?White:Red);
                P("ApronPocket",PrimitiveType.Cube,0,.79f,.293f,.28f,.13f,.025f,chef?White:shirt);
            }
            public void Scarf(Material color)
            {
                P("Scarf",PrimitiveType.Cube,0,1.31f,.15f,.35f,.07f,.27f,color);
                var tail=P("ScarfTail",PrimitiveType.Cube,.09f,1.21f,.258f,.11f,.19f,.035f,color);
                tail.localRotation=Quaternion.Euler(0,0,20);
            }
            public void Glasses()
            {
                foreach(float side in new[]{-1f,1f})
                {
                    // Open frames keep faces readable; no opaque panels hiding the eyes.
                    foreach(float y in new[]{1.58f,1.73f})P("GlassesRim",PrimitiveType.Cube,side*.13f,y,.297f,.22f,.026f,.028f,Ink);
                    foreach(float x in new[]{.025f,.235f})P("GlassesSide",PrimitiveType.Cube,side*x,1.655f,.297f,.027f,.17f,.028f,Ink);
                }
                P("GlassesBridge",PrimitiveType.Cube,0,1.67f,.31f,.075f,.025f,.025f,Ink);
            }
            public void Hair(int style)
            {
                if(style<0)
                {
                    S("Hair",0,1.79f,-.05f,.62f,.18f,.51f,hair);
                    return;
                }
                S("Hair",0,1.86f,-.04f,.66f,.27f,.56f,hair);
                switch(style)
                {
                    case 0:
                        var quiff=S("Quiff",-.12f,1.94f,.12f,.45f,.23f,.36f,hair);quiff.localRotation=Quaternion.Euler(0,0,-15);break;
                    case 1:
                        foreach(float side in new[]{-1f,1f})S("Bob",side*.26f,1.68f,-.09f,.19f,.45f,.45f,hair);
                        S("Fringe",-.12f,1.82f,.20f,.35f,.17f,.20f,hair);break;
                    case 2:
                        S("SidePart",.15f,1.86f,.17f,.35f,.17f,.23f,hair);break;
                    case 3:
                        S("Cap",0,1.92f,0,.72f,.27f,.64f,Accent);
                        S("Brim",0,1.85f,.31f,.68f,.065f,.43f,Accent);break;
                    case 4:
                        S("Beanie",0,1.96f,-.025f,.72f,.34f,.65f,shirt);
                        P("BeanieHem",PrimitiveType.Cylinder,0,1.87f,-.025f,.73f,.045f,.66f,Accent);break;
                    case 5:
                        for(int i=0;i<7;i++)
                        {
                            float a=i*Mathf.PI*2/7;
                            S("Curl",Mathf.Cos(a)*.24f,1.87f+(.07f*(i%2)),Mathf.Sin(a)*.18f,.31f,.29f,.3f,hair);
                        }break;
                    case 6:
                        S("Bun",-.19f,2.00f,-.13f,.35f,.33f,.34f,hair);
                        S("SideFringe",.17f,1.80f,.11f,.25f,.27f,.32f,hair);break;
                    case 7:
                        S("Ponytail",0,1.67f,-.34f,.28f,.50f,.26f,hair);
                        S("HairTie",0,1.84f,-.33f,.29f,.08f,.22f,Red);break;
                }
            }
            public void Finish()
            {
                // Batch fixed face/clothing details by material; do not merge across animated joints.
                owner.Combine(root);
                foreach(string limb in new[]{"LeftArm","RightArm","LeftLeg","RightLeg"})owner.Combine(root.Find(limb));
            }
        }
    }
}
