using SiteSimulation;

namespace SitePresentation;

public partial class OscpWorkspace
{
    private sealed record PhoneDraft(string ActorId, PersonnelTask Task, Hex Destination);
    private readonly List<PhoneDraft> _phoneDrafts = [];
    internal int PhoneDraftCount => _phoneDrafts.Count;
    internal void ClearPhoneDrafts()
    { _phoneDrafts.Clear(); _receipt.Text = "Prepared orders cleared; nothing sent."; Refresh(); }
    private string DraftDescription() => _phoneDrafts.Count == 0 ? "" : "\n\nPREPARED / SEND ON PHONE CLOSE\n"
        + string.Join("\n", _phoneDrafts.Select(d => $"{d.ActorId} / {d.Task}"));
    private void CompletePhoneSession(bool sendDrafts)
    {
        ReleasePhonePause();
        foreach (var draft in _phoneDrafts)
        {
            if (!sendDrafts) continue;
            _sim.SubmitPreparedPhoneOrder(draft.ActorId, draft.Task, draft.Destination);
            _receipt.Text = _sim.LastMessage;
        }
        _phoneDrafts.Clear();
    }
}
