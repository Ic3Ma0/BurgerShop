using System.Collections.Generic;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;
using UnityEngine.UI;
namespace BurgerShop.UI
{
    public enum FeedbackSound { Light, Cash, Spend, Task, Success, Dump }
    public sealed class PickupAccumulator
    {
        public long Amount {get;private set;}
        public float Age {get;private set;}=10;
        public void Add(int amount) { Amount=Age<=.30f?Amount+amount:amount;Age=0; }
        public void Advance(float dt){Age+=Mathf.Max(0,dt);}
    }
    public sealed class FeedbackBudget
    {
        readonly float[] next=new float[8];
        public bool Accept(FeedbackSound sound,float time)
        {
            int i=(int)sound;if(i<0||i>=next.Length||time<next[i])return false;
            next[i]=time+(sound==FeedbackSound.Cash?CashCollectionFeel.SoundInterval:sound==FeedbackSound.Spend?.20f:sound==FeedbackSound.Dump?.09f:.12f);return true;
        }
    }
    [DefaultExecutionOrder(300)]
    public sealed class FeedbackDirector : MonoBehaviour
    {
        public static FeedbackDirector Current {get;private set;}
        public const string SoundPreference="BurgerShop.SoundEnabled";
        public bool SoundEnabled {get;private set;}=true;
        public bool DecorationsEnabled {get;set;}=true;
        public int ActivePrompts => prompts.Count;
        public int ActiveVoices {get{int n=0;foreach(var s in voices)if(s!=null&&s.isPlaying)n++;return n;}}
        public int MaxPromptsSeen {get;private set;}
        public int MaxVoicesSeen {get;private set;}
        public readonly PickupAccumulator Pickups=new PickupAccumulator();
        readonly FeedbackBudget budget=new FeedbackBudget();
        readonly CashSoundSequence cashSequence=new CashSoundSequence();
        float pendingPitch=1f;
        readonly List<Prompt> prompts=new List<Prompt>();
        readonly List<Particle> particles=new List<Particle>();
        readonly AudioSource[] voices=new AudioSource[4];
        readonly AudioClip[] clips=new AudioClip[8];
        AudioSource music;
        AudioClip musicClip;
        RectTransform space; Transform player; Text pickupText; Text soundText;
        int pending=-1;bool paused, applicationPaused, focusLost;float lightUntil;int previousCount;
        BurgerInventory inventory;
        sealed class Prompt {public Image Card;public Text Text;public Vector3 World;public float Age,Life,Priority;public RectTransform[] Stars;}
        sealed class Particle {public Image Image;public Vector2 Origin;public float Age;}
        public static FeedbackDirector Build(Transform parent,BurgerInventory carrier)
        {
            var obj=new GameObject("Feedback",typeof(RectTransform));obj.transform.SetParent(parent,false);
            var r=(RectTransform)obj.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var f=obj.AddComponent<FeedbackDirector>();f.space=r;f.player=carrier.transform;f.inventory=carrier;
            f.previousCount=carrier.Count;carrier.CountChanged+=f.OnItems;
            f.pickupText=HudChrome.Label(parent,"PickupTotal",Vector2.one,Vector2.one,Vector2.one,new Vector2(-40,-120),new Vector2(320,48),32,HudChrome.Green,TextAnchor.MiddleRight,true,false);
            var toggle=HudChrome.Panel(parent,"SoundButton",new Vector2(1,0),new Vector2(1,0),new Vector2(-32,32),new Vector2(176,132),HudChrome.Cream);
            toggle.raycastTarget=true;var button=toggle.gameObject.AddComponent<Button>();button.targetGraphic=toggle;
            f.soundText=HudChrome.Label(toggle.transform,"SoundState",Vector2.zero,Vector2.one,Vector2.one*.5f,Vector2.zero,Vector2.zero,28,HudChrome.Ink,TextAnchor.MiddleCenter,true,false);
            button.onClick.AddListener(()=>f.SetSound(!f.SoundEnabled));
            f.ReadSoundPreference();f.EnsureAudio();f.PaintSound();return f;
        }
        void Awake()
        {
            Current=this;ReadSoundPreference();EnsureAudio();
        }
        void ReadSoundPreference(){SoundEnabled=PlayerPrefs.GetInt(SoundPreference,1)!=0;}
        void EnsureAudio()
        {
            if(music!=null)return;
            for(int i=0;i<4;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].volume=.18f;voices[i].spatialBlend=0;}
            clips[0]=CoinSfx.CreateLight();clips[1]=CoinSfx.CreateCoin();clips[2]=CoinSfx.CreateSpend();clips[3]=CoinSfx.CreateSuccess();clips[4]=clips[3];clips[5]=CoinSfx.CreateDump();
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.volume=1f;music.spatialBlend=0;music.priority=180;
            musicClip=ShopBgm.CreateLoop();music.clip=musicClip;
        }
        public void SetSound(bool enabled)
        {
            SoundEnabled=enabled;PlayerPrefs.SetInt(SoundPreference,enabled?1:0);PlayerPrefs.Save();
            if(!enabled)StopAudio();else SyncMusic();PaintSound();
        }
        void PaintSound(){if(soundText!=null)soundText.text=SoundEnabled?"Sound\nOn":"Sound\nOff";}
        public bool RequestSound(FeedbackSound sound)
        {
            if(paused||!SoundEnabled||!DecorationsEnabled||!budget.Accept(sound,Time.time))return false;
            if((int)sound>=pending)
            {
                pending=(int)sound;
                pendingPitch=sound==FeedbackSound.Cash?cashSequence.Next(Time.time):1f;
            }
            return true;
        }
        void OnItems(int count)
        {
            if(count==previousCount)return;previousCount=count;
            if(Time.time<lightUntil||paused)return;lightUntil=Time.time+.15f;
            RequestSound(FeedbackSound.Light);
            var carry=FindFirstObjectByType<CarryHud>();if(carry!=null)UiPressPulse.Pulse(carry.transform,.06f,.12f);
        }
        public void Cash(Vector3 position,int amount)
        {
            if(paused||!DecorationsEnabled||amount<=0)return;
            Pickups.Add(amount);
            Vector2 from=Project(position);
            // One receipt bill per arrival gives a continuous stream instead of a five-icon burst.
            if(particles.Count<CashCollectionFeel.HudParticleLimit)
            {
                var icon=HudChrome.Icon(transform,"CoinFlight",FoodIcons.Get(FoodIcon.Coin),Vector2.zero,Vector2.one*.5f,from,new Vector2(34,23),Color.white);
                particles.Add(new Particle{Image=icon,Origin=from});
            }
        }
        public bool World(Vector3 position,string text,float seconds,Transform actor=null)
        {
            if(paused||!DecorationsEnabled||space==null)return false;
            float priority=player==null?0:-(position-player.position).sqrMagnitude;
            if(actor==player)priority=10000;
            if(prompts.Count>=3)
            {
                int worst=0;for(int i=1;i<prompts.Count;i++)if(prompts[i].Priority<prompts[worst].Priority)worst=i;
                if(prompts[worst].Priority>priority)return false;
                Destroy(prompts[worst].Card.gameObject);prompts.RemoveAt(worst);
            }
            var card=HudChrome.Panel(transform,"Success",Vector2.zero,Vector2.one*.5f,Vector2.zero,new Vector2(text.Length==0?72:248,72),HudChrome.Cream);
            FoodIcons.Add(card.transform,FoodIcon.Check,new Vector2(text.Length==0?0:-88,0),40);
            var label=HudChrome.Label(card.transform,"Message",Vector2.zero,Vector2.one,Vector2.one*.5f,new Vector2(24,0),new Vector2(-56,0),32,HudChrome.Green,TextAnchor.MiddleCenter,true,false);label.text=text;
            prompts.Add(new Prompt{Card=card,Text=label,World=position+Vector3.up*2.8f,Life=seconds,Priority=priority});
            MaxPromptsSeen=Mathf.Max(MaxPromptsSeen,prompts.Count);return true;
        }
        public void Success(Vector3 position,string text,Transform actor=null)
        {
            bool accepted=World(position,text,.9f,actor);RequestSound(FeedbackSound.Success);
            if(!accepted)return;
            var prompt=prompts[prompts.Count-1];prompt.Stars=new RectTransform[6];
            for(int i=0;i<6;i++)prompt.Stars[i]=HudChrome.Icon(prompt.Card.transform,"Star",HudChrome.Star(),Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.one*20,HudChrome.Gold).rectTransform;
        }
        Vector2 Project(Vector3 world)
        {
            if(Camera.main==null||space==null)return Vector2.zero;
            var p=Camera.main.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space,p,GetComponentInParent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay?null:GetComponentInParent<Canvas>().worldCamera,out var local);
            return local+space.rect.size*.5f;
        }
        void LateUpdate()
        {
            SyncMusic();
            if(paused)return;
            if(pending>=0)
            {
                int slot=-1;for(int i=0;i<4;i++)if(!voices[i].isPlaying){slot=i;break;}
                if(slot<0&&pending>=(int)FeedbackSound.Task)slot=0;
                if(slot>=0&&pending>=0&&pending<clips.Length&&clips[pending]!=null)
                {voices[slot].Stop();voices[slot].pitch=pendingPitch;voices[slot].clip=clips[pending];voices[slot].Play();}
                pending=-1;MaxVoicesSeen=Mathf.Max(MaxVoicesSeen,ActiveVoices);
            }
            float dt=Time.deltaTime;Pickups.Advance(dt);
            if(pickupText!=null){pickupText.text=Pickups.Age<.6f?"+"+Pickups.Amount.ToString("N0"):"";var c=HudChrome.Green;c.a=Mathf.Clamp01((.6f-Pickups.Age)/.2f);pickupText.color=c;}
            for(int i=prompts.Count-1;i>=0;i--)
            {
                var p=prompts[i];p.Age+=dt;if(p.Age>=p.Life){Destroy(p.Card.gameObject);prompts.RemoveAt(i);continue;}
                p.Card.rectTransform.anchoredPosition=Project(p.World)+Vector2.up*p.Age*32;
                if(p.Stars!=null)for(int j=0;j<p.Stars.Length;j++)
                {var star=p.Stars[j];star.gameObject.SetActive(p.Age<.7f);float a=j*Mathf.PI/3;star.anchoredPosition=new Vector2(Mathf.Cos(a)*110,Mathf.Sin(a)*56)*(1+p.Age);}

            }
            for(int i=particles.Count-1;i>=0;i--)
            {
                var p=particles[i];p.Age+=dt;float t=Mathf.Clamp01(p.Age/.4f);
                p.Image.rectTransform.anchoredPosition=Vector2.Lerp(p.Origin,new Vector2(space.rect.width-160,space.rect.height-72),t)+Vector2.up*Mathf.Sin(t*Mathf.PI)*64;
                if(t>=1){Destroy(p.Image.gameObject);particles.RemoveAt(i);}
            }
        }
        void OnApplicationPause(bool value){applicationPaused=value;paused=applicationPaused||focusLost;if(paused)StopAudio();}
        void OnApplicationFocus(bool value){focusLost=!value;paused=applicationPaused||focusLost;if(paused)StopAudio();}
        void StopAudio()
        {
            pending=-1;foreach(var v in voices)if(v!=null)v.Stop();
            if(music!=null)music.Pause();
        }
        void SyncMusic()
        {
            if(music==null||music.clip==null)return;
            bool want=ShopBgm.ShouldPlay(SoundEnabled,DecorationsEnabled,paused,Time.timeScale);
            if(!want){if(music.isPlaying)music.Pause();return;}
            if(music.isPlaying)return;
            if(music.time>0f&&music.time<music.clip.length)music.UnPause();
            else music.Play();
        }
        void OnDestroy()
        {
            if(Current==this)Current=null;if(inventory!=null)inventory.CountChanged-=OnItems;
            for(int i=0;i<clips.Length;i++)
            {
                if(clips[i]==null)continue;
                bool dup=false;
                for(int j=0;j<i;j++)if(clips[j]==clips[i]){dup=true;break;}
                if(!dup)Restaurant.BurgerVisual.Release(clips[i]);
            }
            if(musicClip!=null)Restaurant.BurgerVisual.Release(musicClip);
        }
    }
}
