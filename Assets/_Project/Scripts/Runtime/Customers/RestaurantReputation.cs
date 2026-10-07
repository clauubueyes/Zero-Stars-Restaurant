using System;
using System.Linq;
using System.Text;
using UnityEngine;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Customers
{
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class RestaurantReputation : MonoBehaviour
    {
        [SerializeField] private CustomerServiceLoop _service;
        [SerializeField] private RestaurantDayController _day;
        [SerializeField] private ReputationSettings _settings;
        [SerializeField] private int _randomSeed = 1600;
        public RestaurantReputationState State { get; private set; }
        public int ResolutionRevision => State?.ResolutionCount ?? 0;
        private GameTime Clock => _day != null ? _day.State?.Clock : null;
        public void Validate()
        {
            if (_service == null || _day == null || _settings == null || _service.Reputation != this || _day.Reputation != this)
                throw new ArgumentException("Reputation requires explicit mutual service/day references and settings.");
            _settings.CreatePolicy();
        }
        private void Awake()
        {
            try { Validate(); var random = new System.Random(_randomSeed); State = new RestaurantReputationState(_settings.CreatePolicy(), random.NextDouble); }
            catch (ArgumentException exception) { Debug.LogError("Invalid reputation configuration: " + exception.Message, this); enabled = false; }
        }
        public void RegisterService(CustomerConsequence consequence)
        { if (State != null && Clock != null) State.TryRegisterService(consequence, Clock); }
        public void CompleteVisit(CustomerConsequence consequence)
        { if (State != null && Clock != null) State.TryCompleteVisit(consequence, Clock); }
        public void ResolveDue()
        { if (isActiveAndEnabled && State != null && Clock != null) State.ResolveDue(Clock); }
        [ContextMenu("Development: Resolve Departed Risks Now (RNG)")]
        public void ResolveDepartedNow() => ResolveDevelopment(null);
        [ContextMenu("Development: Confirm Departed Health Incidents Now")]
        public void ConfirmDepartedNow() => ResolveDevelopment(true);
        [ContextMenu("Development: Dismiss Departed Health Risks Now")]
        public void DismissDepartedNow() => ResolveDevelopment(false);
        private void ResolveDevelopment(bool? confirmed)
        { if (State != null && Clock != null) State.ResolveDepartedNow(Clock, confirmed); }

        public static string EventText(ReputationEvent entry)
        {
            if (entry == null) return "None";
            string title = entry.Kind == ReputationEventKind.HealthIncidentConfirmed ? "Customer reported illness" :
                entry.Kind == ReputationEventKind.HealthRiskDismissed ? "Health risk resolved: no additional consequence" : entry.Kind.ToString();
            var text = new StringBuilder(title + " | Reputation: " + entry.Before + " → " + entry.After);
            text.Append("\nDay " + entry.DayNumber + " | Customer #" + entry.CustomerId.ToString("N").Substring(0, 8));
            if (entry.Kind == ReputationEventKind.HealthIncidentConfirmed || entry.Kind == ReputationEventKind.HealthRiskDismissed)
                foreach (var cause in entry.Consequence.Quality.Causes.Where(c => c.IsHealthHazard))
                    text.Append("\nCause: " + cause.Issue + " " + cause.Ingredient.Profile.DisplayName + " #" + cause.Ingredient.InstanceId.ToString("N").Substring(0, 8));
            if (entry.Forced) text.Append("\nDevelopment: forced outcome");
            return text.ToString();
        }
        public string LastResolutionMessage => EventText(State?.LastResolution);
        public string Text
        {
            get
            {
                if (State == null) return "";
                var text = new StringBuilder("Reputation: " + State.Value + " (" + State.Policy.Minimum + "–" + State.Policy.Maximum + ")" +
                    "\nPending health risks: " + State.PendingCount + " | Resolved: " + State.ResolutionCount);
                text.Append("\nLast change: " + EventText(State.LastChange));
                text.Append("\nLast delayed consequence: " + LastResolutionMessage);
                foreach (var risk in State.HealthRisks.Where(r => !r.IsResolved))
                    text.Append("\nRisk #" + risk.CustomerId.ToString("N").Substring(0, 8) + " | " + risk.Probability.ToString("P0") +
                        (risk.DueWorldSeconds.HasValue ? " | Remaining GameTime: " + Math.Max(0, risk.DueWorldSeconds.Value - Clock.ElapsedWorldSeconds).ToString("F0") + "s" : " | Awaiting departure"));
                return text.ToString();
            }
        }
    }
}
