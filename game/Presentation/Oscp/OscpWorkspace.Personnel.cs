using SiteSimulation;

namespace SitePresentation;

public partial class OscpWorkspace
{
    private string PersonnelDocument()
    {
        if (_sim.Employment.Definition is not { } definition)
            return string.Join("\n\n", _sim.Actors.Where(a => a != _sim.Player).Select(a => a.Name + " / " + a.Id + "\nAssigned to current site operation"));
        var entries = definition.Appointments.Select(a =>
        {
            var actor = _sim.Actors.Single(actor => actor.Id == a.ActorId);
            var report = _sim.Employment.DeliveredReports.SingleOrDefault(r => r.ActorId == a.ActorId);
            var post = definition.Posts.Single(p => p.Id == a.PostId);
            return $"{actor.Name} / {actor.Id}\n{a.Role} · {post.Name}\n"
                + $"Local duty cycle {a.CycleMs / 1000}s · starts {a.StartMs / 1000}s · lasts {a.DurationMs / 1000}s · overtime limit {a.MaxOvertimeMs / 1000}s\n"
                + (report is null ? "Awaiting employment report." : $"LAST REPORT L {report.LocalMs / 1000.0:0.0}s: {report.Phase}\n{report.Reason}");
        });
        var coverage = definition.Posts.Select(post =>
        {
            var reports = _sim.Employment.DeliveredReports.Where(r => definition.Appointments.Any(a => a.ActorId == r.ActorId && a.PostId == post.Id));
            var count = reports.Count(r => r.Phase is DutyPhase.OnDuty or DutyPhase.AwaitingRelief);
            return $"{post.Name}: reported {count} / required {post.MinimumStaff}"
                + (count < post.MinimumStaff ? " · STAFFING WARNING" : "") + "\nLast reports only; not live observation or containment certification.";
        });
        return string.Join("\n\n", entries) + "\n\nPOST COVERAGE / REPORTED\n" + string.Join("\n\n", coverage)
            + "\n\nDirect orders suspend employment only on delivery. Return to duty releases a standing command. Test shifts use local execution time; tactical holds freeze them.";
    }
}
