using System;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.Rendering;

namespace BurgerShop.Customer
{
    [ExecuteAlways]
    public sealed class CustomerAgent : MonoBehaviour
    {
        Transform orderBubble;
        Material[] ownedMaterials;
        Transform carriedBurger;
        readonly System.Collections.Generic.List<Transform> received = new System.Collections.Generic.List<Transform>();
        Vector3 burgerStart;
        Vector3[] exitRoute;
        int exitWaypoint;
        bool exitPathBuilt;
        int exitLayoutRevision=-1;
        float departureTime;
        DiningArea hall;
        DiningTable table;
        Vector3 seatPosition;
        int seatIndex = -1;
        float eatTime;
        Phase phase;
        const float HandoffSeconds = 0.4f;
        const float DepartureSpeed = 1.92f;

        enum Phase { None, Handoff, ToSeat, Seating, Eating, LeavingSeat, Restroom, Exiting }
        int restroomSlot=-1,restroomStage;float restroomTime,restroomWait;
        public bool IsUsingRestroom=>phase==Phase.Restroom;

        int lockedMealTip = 10;
        public void LockMealTip(int tip) => lockedMealTip=tip;
        public CustomerKind Kind { get; private set; }
        public float CallingRemaining { get; private set; }
        public float ReminderProgress { get; private set; }
        bool callStarted, callFinished;
        public bool CanAcceptOrder => Kind != CustomerKind.Calling || callFinished;

        public int TicketNumber { get; private set; }
        public int QueueIndex { get; private set; }
        public CustomerOrder Order { get; private set; } = new CustomerOrder(1);
        public int OrderSize => Order.Quantity;
        public int RemainingQuantity => Order.Remaining;
        public float DistanceAlongPath { get; private set; }
        public bool HasReachedSlot { get; private set; }
        public bool HasOrdered { get; private set; }
        public bool IsDeparting { get; private set; }
        public bool IsDining => phase == Phase.ToSeat || phase == Phase.Seating || phase == Phase.Eating || phase == Phase.LeavingSeat;
        public bool IsEating => phase == Phase.Eating;
        public bool DepartureComplete { get; private set; }
        public int PaidAmount { get; private set; }
        public event Action<CustomerAgent> Removed;

        public KitchenProduct Product { get; private set; }

        internal static CustomerAgent Create(Transform parent, int ticket, Vector3 entrance, int quantity = 1, CustomerKind kind = CustomerKind.Normal, KitchenProduct product = KitchenProduct.Burger)
        {
            GameObject root = new GameObject($"Customer_{ticket}");
            root.transform.SetParent(parent, false);
            root.transform.position = entrance;
            CustomerAgent agent = root.AddComponent<CustomerAgent>();
            agent.TicketNumber = ticket;
            agent.Kind = kind;
            agent.Product = product;
            if (kind == CustomerKind.BigEater) quantity = 10;
            agent.Order = new CustomerOrder(quantity, kind == CustomerKind.BigEater ? 10 : 4);
            Material bubble = MaterialFor(new Color(1f, 0.98f, 0.90f), true);
            agent.ownedMaterials = new[] { bubble };
            BurgerShop.Core.CharacterVisualFactory.Customer(root.transform, ticket,
                kind == CustomerKind.BigEater, kind == CustomerKind.Calling);
            agent.orderBubble = new GameObject("OrderBubble").transform;
            agent.orderBubble.SetParent(root.transform, false);
            agent.orderBubble.localPosition = new Vector3(0f, 2.25f, 0f);
            Part(agent.orderBubble, "Background", PrimitiveType.Cube, Vector3.zero, new Vector3(1.15f, 0.55f, 0.035f), bubble);
            Transform icon = product == KitchenProduct.Cola
                ? ColaVisualFactory.Create(agent.orderBubble, 0)
                : BurgerVisualFactory.Create(agent.orderBubble, 0);
            icon.name = product == KitchenProduct.Cola ? "ColaIcon" : "BurgerIcon";
            icon.localPosition = new Vector3(-0.28f, -0.12f, -0.18f);
            icon.localScale = Vector3.one * 0.65f;
            TextMesh label = new GameObject("OrderQuantity").AddComponent<TextMesh>();
            label.transform.SetParent(agent.orderBubble, false);
            label.transform.localPosition = new Vector3(0.2f, 0f, -0.06f);
            label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.12f;
            label.fontSize = 48;
            label.color = new Color(0.22f, 0.15f, 0.08f);
            label.text = "x" + quantity;
            foreach (Renderer renderer in agent.orderBubble.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            agent.orderBubble.gameObject.SetActive(false);
            return agent;
        }

        internal void AssignSlot(int index)
        {
            QueueIndex = index;
            HasReachedSlot = false;
        }

        Vector3[] rejoin;
        int rejoinStep, rejoinRevision=-1;
        Vector3 rejoinTarget;
        float rejoinRetry;
        internal void MoveOnPath(Vector3 position, float distance, bool arrived, Vector3 counter, float deltaTime)
        {
            // A layout change updates logical progress, never teleports a live guest.
            if(deltaTime<=0){DistanceAlongPath=distance;HasReachedSlot=false;rejoin=null;return;}
            float budget=3.5f*deltaTime;
            if(Vector3.Distance(position,transform.position)>budget+.01f || !ActorObstacles.Clear(transform.position,position) || rejoin!=null)
            {
                int revision=Building.FacilityLayout.Current?.Revision??-1;
                rejoinRetry-=deltaTime;
                if(rejoin==null||rejoinTarget!=position||rejoinRevision!=revision)
                {
                    if(rejoin==null&&rejoinRetry>0)return;
                    rejoin=ActorObstacles.Route(transform.position,position);
                    if(rejoin==null&&ActorObstacles.Clear(transform.position,position))rejoin=new[]{position};
                    rejoinStep=0;rejoinTarget=position;rejoinRevision=revision;rejoinRetry=.5f;
                }
                if(rejoin==null)return;
                while(rejoinStep<rejoin.Length)
                {
                    if(!StepDirect(rejoin[rejoinStep],ref budget))return;
                    rejoinStep++;
                }
                rejoin=null;
                if(Vector3.Distance(transform.position,position)>.05f)return;
            }
            Vector3 displacement=position-transform.position;displacement.y=0;
            Vector3 direction = displacement.sqrMagnitude>.000001f ? displacement : arrived ? counter-position : Vector3.zero;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.00001f)
                transform.rotation = displacement.sqrMagnitude>.000001f ? Quaternion.LookRotation(direction) : Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-8f * deltaTime));
            transform.position = position;
            DistanceAlongPath = distance;
            HasReachedSlot = arrived;
            if (arrived && !HasOrdered)
            {
                HasOrdered = true;
                orderBubble.gameObject.SetActive(true);
            }
        }

        public void AdvanceCalling(float seconds, Transform player)
        {
            if (seconds <= 0 || Kind != CustomerKind.Calling || callFinished || QueueIndex != 0 || !HasReachedSlot) return;
            if (!callStarted) { callStarted = true; CallingRemaining = 8f; }
            else CallingRemaining = Mathf.Max(0, CallingRemaining - seconds);
            bool near = player != null && ShopLayout.Horizontal(player.position, transform.position) <= 1.5f;
            ReminderProgress = near ? ReminderProgress + seconds : 0f;
            if (CallingRemaining <= 0 || ReminderProgress >= 1f)
            {
                callFinished = true; CallingRemaining = 0;
                orderBubble.GetComponentInChildren<TextMesh>(true).text = "x" + RemainingQuantity;
                UI.FeedbackDirector.Current?.World(transform.position, "", .45f);
            }
            else orderBubble.GetComponentInChildren<TextMesh>(true).text = (near ? "Remind " : "Calling ") + Mathf.CeilToInt(CallingRemaining);
        }

        internal void LeaveQueue()
        {
            QueueIndex = -1;
            HasReachedSlot = false;
            orderBubble.gameObject.SetActive(false);
        }

        internal void ReceiveItem(Transform burger)
        {
            if (!Order.Receive()) return;
            if (burger != null)
            {
                burger.SetParent(transform, true);
                burger.localPosition = new Vector3(0, 0.85f + received.Count * 0.15f, 0.6f);
                burger.localRotation = Quaternion.identity;
                received.Add(burger);
            }
            orderBubble.GetComponentInChildren<TextMesh>(true).text = "x" + Order.Remaining;
        }

        internal void BeginDeparture(Transform burger, Vector3[] waypoints, int payment, DiningArea dining = null,
            bool alreadyReceived = false)
        {
            IsDeparting = true;exitWaypoint=0;exitPathBuilt=false;
            departureTime = alreadyReceived ? HandoffSeconds : 0f;
            if (burger != null && !received.Contains(burger)) received.Add(burger);
            PaidAmount = payment;
            exitRoute = waypoints != null ? (Vector3[])waypoints.Clone() : Array.Empty<Vector3>();
            carriedBurger = burger;
            hall = dining;
            table = null;
            seatIndex = -1;
            eatTime = 0f;
            if (burger != null)
            {
                burger.SetParent(transform, true);
                burgerStart = burger.localPosition;
            }
            orderBubble.Find("Background").gameObject.SetActive(false);
            Transform foodIcon = orderBubble.Find("BurgerIcon") ?? orderBubble.Find("ColaIcon");
            if (foodIcon != null) foodIcon.gameObject.SetActive(false);
            TextMesh receipt = orderBubble.GetComponentInChildren<TextMesh>(true);
            receipt.transform.localPosition = Vector3.zero;
            receipt.color = new Color(1f, 0.78f, 0.12f);
            receipt.text = "";
            orderBubble.gameObject.SetActive(true);
            phase = Phase.Handoff;
            if (hall != null && !hall.TryAssignSeat(this, out table, out seatPosition, out seatIndex))
            {
                // Dirty or full hall: still leave the counter. Wait by the tables when
                // they exist; otherwise skip dining and walk out the door.
                if (table == null && hall.TableCount == 0)
                    hall = null;
            }
        }

        void Update()
        {
            if (Application.isPlaying) AdvanceDeparture(Time.deltaTime);
        }

        public void AdvanceDeparture(float deltaTime)
        {
            Vector3 before=transform.position;
            AdvanceDepartureMotion(deltaTime);
            if(this==null||phase==Phase.Eating)return;
            Vector3 displacement=transform.position-before;displacement.y=0;
            // A frame may cross several short curve segments: face the displacement of the
            // entire frame, never a future waypoint that has not been walked toward yet.
            if(displacement.sqrMagnitude>.000001f)transform.rotation=Quaternion.LookRotation(displacement);
        }

        void AdvanceDepartureMotion(float deltaTime)
        {
            if (!IsDeparting || DepartureComplete || deltaTime <= 0f) return;
            float previousTime = departureTime;
            departureTime += deltaTime;
            if (carriedBurger != null && phase != Phase.Eating && phase != Phase.Exiting)
            {
                float t = Mathf.Clamp01(departureTime / HandoffSeconds);
                carriedBurger.localPosition = Vector3.Lerp(burgerStart, new Vector3(0f, 0.85f, 0.6f), t)
                    + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.6f);
                carriedBurger.localRotation = Quaternion.Slerp(carriedBurger.localRotation, Quaternion.identity, t);
            }
            if (departureTime > 1.4f) orderBubble.gameObject.SetActive(false);

            if (phase == Phase.Handoff)
            {
                if (departureTime < HandoffSeconds) return;
                phase = hall != null || table != null ? Phase.ToSeat : Phase.Exiting;
            }

            float remaining = DepartureSpeed * (phase == Phase.Exiting && previousTime < HandoffSeconds
                ? Mathf.Max(0f, departureTime - HandoffSeconds) - Mathf.Max(0f, previousTime - HandoffSeconds)
                : deltaTime);

            float mealDelta=deltaTime;
            if (phase == Phase.ToSeat)
            {
                if (seatIndex < 0)
                {
                    if (hall != null)
                        hall.TryAssignSeat(this, out table, out seatPosition, out seatIndex);
                    else if (table != null && table.gameObject.activeInHierarchy && !table.IsDirty)
                        table.TryAssignSeat(this, out seatPosition, out seatIndex);
                }
                Vector3 wait = table != null ? table.WaitPosition : (hall != null ? hall.WaitPosition : transform.position);
                if (seatIndex >= 0 && table != null) seatPosition = table.SeatPosition(seatIndex);
                Vector3 target = seatIndex >= 0 ? table.SeatApproach(seatIndex) : wait;
                if (!StepToward(target, ref remaining)) return;
                if (seatIndex < 0)
                {
                    FaceTable();
                    return;
                }
                FaceTable();
                LockMealTip(table.MealTip);
                seatedApproach=transform.position;
                phase = Phase.Seating;
                eatTime = 0f;
            }
            if(phase==Phase.Seating)
            {
                if(table==null){phase=Phase.Exiting;return;}
                if(!MoveAtChair(table.SeatPosition(seatIndex),ref remaining))return;
                phase=Phase.Eating;
                mealDelta=remaining/DepartureSpeed; // Only leftover time counts as eating.
            }

            if (phase == Phase.Eating)
            {
                if (table != null && seatIndex >= 0)
                { FaceTable(); PlaceBurgerOnTable(); }
                float previousEatTime=eatTime;
                eatTime += mealDelta;
                float need = (table != null ? table.EatSeconds : 5f) * (Kind == CustomerKind.BigEater ? 1.5f : 1f);
                if (eatTime < need) return;
                int finishedSeat = seatIndex;
                DiningTable finishedTable = table;
                if (finishedTable != null && finishedSeat >= 0)
                {
                    finishedTable.LeaveMealTrash(finishedSeat);
                    finishedTable.LeaveMealCash(lockedMealTip);
                }
                ReleaseMeal();
                phase=Phase.LeavingSeat;
                remaining=DepartureSpeed*Mathf.Max(0,mealDelta-Mathf.Max(0,need-previousEatTime));
            }
            if(phase==Phase.LeavingSeat)
            {
                if(!MoveAtChair(seatedApproach,ref remaining))return;
                table?.Release(this);
                seatIndex = -1;
                if (exitRoute.Length > 0 && transform.position.x > ShopLayout.WallHalf)
                {
                    var leaving = new System.Collections.Generic.List<Vector3>(ShopLayout.WingRoute(transform.position, exitRoute[0]));
                    for (int i = 1; i < exitRoute.Length; i++) leaving.Add(exitRoute[i]);
                    exitRoute = leaving.ToArray(); exitWaypoint = 0;
                }
                phase = RestroomExpansion.Current?.TryVisit(this)==true?Phase.Restroom:Phase.Exiting;
            }
            if(phase==Phase.Restroom)
            {
                if(TickRestroom(deltaTime,ref remaining))return;
                phase=Phase.Exiting;wingWalk=null;
            }

            int revision=Building.FacilityLayout.Current?.Revision??-1;
            if(!exitPathBuilt||exitLayoutRevision!=revision)
            {
                var path=new System.Collections.Generic.List<Vector3>{transform.position};
                if(exitRoute.Length==0)return;
                var outgoing=ShopLayout.Walk(transform.position,exitRoute[exitRoute.Length-1]);
                if(outgoing.Length==0)return;
                path.AddRange(outgoing);
                exitRoute=new CustomerWalkPath(path,TicketNumber).Points;exitWaypoint=0;exitPathBuilt=true;exitLayoutRevision=revision;
            }
            while (remaining > 0f && exitWaypoint < exitRoute.Length)
            {
                if(!StepDirect(exitRoute[exitWaypoint],ref remaining))break;
                exitWaypoint++;
            }
            if (exitWaypoint == exitRoute.Length)
            {
                DepartureComplete = true;
                gameObject.SetActive(false);
                BurgerVisual.Release(gameObject);
            }
        }

        bool TickRestroom(float seconds,ref float travel)
        {
            var room=RestroomExpansion.Current;if(room==null||!room.Built)return false;
            if(restroomStage==0)
            {
                if(!StepToward(RestroomExpansion.Door,ref travel))return true;
                restroomStage=1;
            }
            if(restroomStage==1)
            {
                restroomSlot=room.Acquire(this);
                if(restroomSlot<0)
                {
                    StepToward(room.WaitPoint(this),ref travel);restroomWait+=seconds;
                    if(restroomWait>RestroomExpansion.MaxWaitSeconds){room.Release(this);return false;}return true;
                }
                restroomStage=2;
            }
            if(restroomStage==2)
            {
                if(!StepToward(RestroomExpansion.UsePoint(restroomSlot),ref travel))return true;
                restroomTime+=seconds;if(restroomTime<RestroomExpansion.UseSeconds)return true;
                room.FinishUse(this,restroomSlot);restroomTime=0;restroomStage=3;
            }
            if(restroomStage==3)
            {
                if(!StepToward(RestroomExpansion.Wash,ref travel))return true;
                restroomTime+=seconds;if(restroomTime<RestroomExpansion.WashSeconds)return true;
                restroomStage=4;
            }
            if(!StepToward(RestroomExpansion.Door+Vector3.forward,ref travel))return true;
            room.Release(this);return false;
        }
        bool MoveAtChair(Vector3 target,ref float remaining)
        {
            target.y=transform.position.y;
            var next=Vector3.MoveTowards(transform.position,target,remaining);
            // A seated posture can enter its assigned chair, never another chair or table.
            if(!ActorObstacles.Clear(transform.position,next,table?.SeatChair(seatIndex),.2f))return false;
            float moved=Vector3.Distance(transform.position,next);
            transform.position=next;remaining=Mathf.Max(0,remaining-moved);
            FaceTable();
            return Vector3.Distance(next,target)<.0001f;
        }
        Vector3 seatedApproach;
        Vector3[] wingWalk;
        Vector3 wingTarget;
        int wingStep;
        int layoutRevision=-1;
        bool StepToward(Vector3 target, ref float travel)
        {
            if (wingWalk == null || wingTarget != target || layoutRevision != (Building.FacilityLayout.Current?.Revision??-1))
            {
                wingTarget = target;
                layoutRevision=Building.FacilityLayout.Current?.Revision??-1;
                wingWalk = PlannedWalk(transform.position, target);
                if (wingWalk == null || wingWalk.Length == 0) {wingWalk=null;return false;}
                wingStep = 0;
            }
            while (wingStep < wingWalk.Length)
            {
                if (!StepDirect(wingWalk[wingStep], ref travel)) return false;
                wingStep++;
            }
            return true;
        }

        Vector3[] PlannedWalk(Vector3 from, Vector3 to)
        {
            var route=ShopLayout.Walk(from,to);
            if(route.Length==0)return null;
            var points=new System.Collections.Generic.List<Vector3>{from};points.AddRange(route);
            return new CustomerWalkPath(points,TicketNumber).Points;
        }

        bool StepDirect(Vector3 target, ref float travel)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            Vector3 next=Vector3.MoveTowards(transform.position,new Vector3(target.x,transform.position.y,target.z),travel);
            if(!ActorObstacles.Clear(transform.position,next)){wingWalk=null;exitPathBuilt=false;rejoin=null;travel=0;return false;}
            if (distance <= .00001f) return true;
            if (distance > 0.0001f)
                transform.rotation = Quaternion.LookRotation(offset);
            if (travel < distance)
            {
                transform.position += offset.normalized * travel;
                travel = 0f;
                return false;
            }
            transform.position = new Vector3(target.x, transform.position.y, target.z);
            travel -= distance;
            return true;
        }

        void FaceTable()
        {
            if (table == null) return;
            Vector3 look = table.Center - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);
        }

        void PlaceBurgerOnTable()
        {
            if (carriedBurger == null || table == null) return;
            carriedBurger.SetParent(transform, true);
            Vector3 toward = table.Center - transform.position;
            toward.y = 0f;
            Vector3 place = table.Center + Vector3.up * 0.86f;
            if (toward.sqrMagnitude > 0.0001f) place += toward.normalized * -0.22f;
            carriedBurger.position = place;
            carriedBurger.rotation = Quaternion.identity;
            int index = 0;
            foreach (Transform food in received)
            {
                if (food == null || food == carriedBurger) continue;
                food.SetParent(transform, true);
                food.position = place + new Vector3(0.24f * (++index), 0, 0);
                food.rotation = Quaternion.identity;
            }
        }

        void ReleaseMeal()
        {
            foreach (Transform food in received)
                if (food != null) BurgerVisual.Release(food.gameObject);
            received.Clear();
            carriedBurger = null;
        }

        void LateUpdate()
        {
            if (orderBubble != null && Camera.main != null)
                orderBubble.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy()
        {
            // Meal visuals remain owned children; Unity tears them down with the customer.
            RestroomExpansion.Current?.Release(this);
            table?.Release(this);
            Removed?.Invoke(this);
            if (ownedMaterials != null)
                foreach (Material material in ownedMaterials)
                    if (material != null) BurgerVisual.Release(material);
        }

        static Material MaterialFor(Color color, bool unlit = false) => BurgerShop.Core.RuntimeMaterials.Create(color, unlit);

        static void Part(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }
    }
}
