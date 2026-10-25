using SiteSimulation;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class EmploymentChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Employment: " + detail); passed++; }
        var definition = JsonSerializer.Deserialize<OperationDefinition>(config, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var employment = definition.Employment!;
        var chen = employment.Appointments[0] with { CycleMs = 10000, StartMs = 0, DurationMs = 3000, MaxOvertimeMs = 2000 };
        var ortiz = employment.Appointments[1] with { CycleMs = 10000, StartMs = 0, DurationMs = 9000, MaxOvertimeMs = 500 };
        string Config(StaffAppointment? first = null, StaffAppointment? second = null) => JsonSerializer.Serialize(definition with
        { Employment = employment with { Appointments = [first ?? chen, second ?? ortiz] } });
        SpatialSimulation Sim(string? fixture = null) => new(map, fixture ?? Config());
        void Ticks(SpatialSimulation sim, int count) { for (var i = 0; i < count; i++) sim.Tick(); }
        StaffDuty Duty(SpatialSimulation sim, string id = "observer") => sim.Employment.DiagnosticDuties.Single(s => s.ActorId == id);
        var normal = Sim(); normal.Tick();
        Check(normal.Operations.OrderHistory.Count == 0 && normal.DiagnosticPostCoverage("receiving-watch") == 2,
            "on-post employment supplies real physical coverage without invented orders");
        Check(normal.Employment.DeliveredReports.Count == 2 && normal.OscpRecords.Any(r => r.Text.StartsWith("Employment:")),
            "personnel publish actual dated status through the communication service");
        var interrupted = Sim(); interrupted.Tick();
        interrupted.Operations.NetworkOnline = false;
        Check(interrupted.SubmitPreparedPhoneOrder("observer", PersonnelTask.Post, new(8, 4)) == false,
            "offline phone cannot submit a new command");
        interrupted.Operations.NetworkOnline = true;
        interrupted.Operations.Radios[0] = new("observer", false);
        Check(interrupted.SubmitPreparedPhoneOrder("observer", PersonnelTask.Post, new(8, 4)), "linked handset can queue a command for an offline recipient");
        interrupted.Tick();
        Check(interrupted.Operations.OrderHistory.Last().Status == OrderStatus.Pending && Duty(interrupted).Phase == DutyPhase.OnDuty,
            "undelivered command cannot erase employment or release a post");
        interrupted.Operations.Radios[0] = new("observer"); interrupted.Tick();
        Check(Duty(interrupted).Phase == DutyPhase.Interrupted && interrupted.DiagnosticPostCoverage("receiving-watch") == 1,
            "acknowledged reassignment removes employment coverage even before leaving the station");
        Check(interrupted.OscpRecords.Any(r => r.Text.Contains("reassignment may leave")), "director receives explicit coverage consequence in acknowledgement");
        Check(interrupted.SubmitPreparedPhoneOrder("observer", PersonnelTask.ResumeDuty, interrupted.Player.Cell), "director can release a standing command");
        Ticks(interrupted, 25);
        Check(interrupted.Operations.OrderHistory.Last().Status == OrderStatus.Complete && Duty(interrupted).Phase == DutyPhase.OnDuty,
            "acknowledged return command completes and staff physically return to scheduled station");
        Check(interrupted.Actors.Single(a => a.Id == "observer").Cell == chen.Station && !interrupted.Actors.Single(a => a.Id == "observer").Moving,
            "employment cannot clock in remotely");

        var moving = Sim(); moving.Map.OpenDoors.Add("door-4-3-u"); moving.Tick();
        Check(moving.IssueOrder("tech", PersonnelTask.Post, new(8, 4)), "direct order can temporarily move an employee away from duty");
        Ticks(moving, 3);
        var mover = moving.Actors.Single(a => a.Id == "tech");
        Check(mover.Moving && mover.SegmentMs > 0, "interruption fixture genuinely reaches a fractional walking step");
        var positionBeforeReturn = mover.Position;
        Check(moving.SubmitPreparedPhoneOrder("tech", PersonnelTask.ResumeDuty, moving.Player.Cell), "return command can interrupt travel");
        moving.Tick();
        Check(Point2.Distance(positionBeforeReturn, mover.Position) <= 0.13, "return-to-duty route reverses from the real fractional position without snapping");
        var movingClone = Sim(); movingClone.Restore(moving.Save());
        for (var i = 0; i < 25; i++) { moving.Tick(); movingClone.Tick(); }
        Check(moving.Save() == movingClone.Save() && Duty(moving, "tech").Phase == DutyPhase.OnDuty,
            "saving during interrupted travel retains exact physical return and employee ownership");

        var handoff = Sim(Config(second: ortiz with { StartMs = 2000, DurationMs = 7000 }));
        handoff.Tick();
        var beforeHandoff = handoff.Save(); var beforeHandoffClone = Sim(Config(second: ortiz with { StartMs = 2000, DurationMs = 7000 }));
        beforeHandoffClone.Restore(beforeHandoff);
        Check(beforeHandoffClone.Save() == beforeHandoff && Duty(handoff, "tech").ReliefSinceMs is null,
            "initial off-shift staff have no imaginary pre-campaign overtime");
        Ticks(handoff, 58);
        Check(Duty(handoff, "tech").Phase == DutyPhase.OnDuty, "incoming employee travels from rest before providing relief");
        Ticks(handoff, 2);
        Check(Duty(handoff).Phase == DutyPhase.OffShift && handoff.DiagnosticPostCoverage("receiving-watch") >= 1,
            "shift ends only after a real replacement is ready; no gap or imaginary observer");
        Ticks(handoff, 20);
        Check(handoff.Actors.Single(a => a.Id == "observer").Cell == chen.Rest, "released employee actually travels to rest");

        Ticks(handoff, 45);
        Check(Duty(handoff).Release == DutyRelease.Relieved && !Duty(handoff).Reason.Contains("Overtime limit"),
            "a successful handover cannot later be misreported as expired overtime");

        var absent = Sim(); absent.Tick(); absent.ToggleNearestDoor();
        Check(absent.IssueOrder("tech", PersonnelTask.Post, new(4, 6)), "authorized director may knowingly reassign the relief worker");
        Ticks(absent, 65);
        Check(Duty(absent).Phase == DutyPhase.AwaitingRelief && Duty(absent).ReliefSinceMs == 3000,
            "missing relief starts bounded overtime at the actual local shift edge");
        Check(absent.DiagnosticPostCoverage("receiving-watch") == 1, "overtime counts only the actual ready observer");
        var saved = absent.Save(); var clone = Sim(); clone.Restore(saved);
        Check(clone.Save() == saved, "pending relief saves without replaying reports or resetting overtime");
        for (var i = 0; i < 70; i++) { absent.Tick(); clone.Tick(); }
        Check(absent.Save() == clone.Save(), "restored relief limit and departure match uninterrupted execution");
        Check(Duty(absent).Phase == DutyPhase.OffShift && absent.DiagnosticPostCoverage("receiving-watch") == 0,
            "overtime expiry allows real uncovered post; staff are not compelled forever");
        Check(absent.Anomalies.Bodies.Single().Revision == 0,
            "staffing failure cannot force SCP relocation while a qualifying player still sees it");

        absent.Face(new Hex(5, 8).World);
        Ticks(absent, 20);
        Check(absent.Anomalies.Bodies.Single().Revision > 0,
            "ordinary shift departure plus lost remaining sight can trigger the module's actual relocation without a mission script");

        var held = Sim(); held.Tick(); held.LocalHold = true;
        var local = held.LocalMs; var before = JsonSerializer.Serialize(held.Employment.DiagnosticDuties);
        Ticks(held, 200);
        Check(held.WorldMs > 10000 && held.LocalMs == local && JsonSerializer.Serialize(held.Employment.DiagnosticDuties) == before,
            "local holds retain duties/relief while the world calendar advances");
        held.LocalHold = false; held.Tick();
        Check(Duty(held).Phase == DutyPhase.OnDuty, "resuming does not replay world-calendar labor or consume a local test shift");
        held.ManualPause = true; var paused = held.Save(); Ticks(held, 100);
        Check(held.Save() == paused, "phone/manual pause freezes all employment and reports");

        var blocked = Sim(Config(first: chen with { Station = new(8, 4) })); blocked.Tick();
        Check(Duty(blocked).Phase == DutyPhase.Blocked && blocked.DiagnosticPostCoverage("receiving-watch") == 1,
            "closed access produces blocked duty rather than teleportation or discretionary patrol");
        var blockedSave = blocked.Save(); var blockedClone = Sim(Config(first: chen with { Station = new(8, 4) })); blockedClone.Restore(blockedSave);
        Check(blockedClone.Save() == blockedSave, "blocked duty retains exact continuation");
        var blind = Sim(); blind.Tick(); blind.Actors.Single(a => a.Id == "observer").Attending = false; blind.Tick();
        Check(Duty(blind).Phase == DutyPhase.Blocked && blind.DiagnosticPostCoverage("receiving-watch") == 1,
            "being assigned and physically present cannot substitute for readiness");
        var logs = blind.Trace.Entries.Count; Ticks(blind, 8);
        Check(blind.Trace.Entries.Count - logs < 8, "stable blocked employment must not toggle/flood duty transitions every tick");

        var offline = Sim(); offline.Tick();
        offline.Operations.NetworkOnline = false;
        // Both voice and radio paths are lost. Keep the director's actual knowledge frozen.
        offline.Player.Cell = new(5, 8);
        var reports = JsonSerializer.Serialize(offline.Employment.DeliveredReports);
        offline.Actors.Single(a => a.Id == "observer").Attending = false; offline.Tick();
        Check(Duty(offline).Phase == DutyPhase.Blocked && JsonSerializer.Serialize(offline.Employment.DeliveredReports) == reports,
            "OSCP keeps old personnel reports instead of reading hidden live state during link loss");
        offline.Operations.NetworkOnline = true; offline.Tick();
        Check(offline.Employment.DeliveredReports.Single(r => r.ActorId == "observer").Phase == DutyPhase.Blocked,
            "link restoration sends current pending employment state without replaying missed labor");

        var tactical = Sim(); tactical.Tick(); tactical.Player.Facing = 1;
        Check(tactical.ArmTraining() && tactical.Combat.Active, "employment fixture enters the existing tactical encounter");
        tactical.Combat.Roster.RemoveAll(c => c.ActorId == "observer"); // Reserve only the drill participants in this fixture.
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        tactical.Combat.Roster[tactical.Combat.Cursor] = tactical.Combat.Roster[tactical.Combat.Cursor] with { Ap = 10 };
        Check(tactical.DiagnosticPostCoverage("receiving-watch") == 1, "a tactical participant cannot simultaneously cover an employment post");
        var dutyBeforeTurn = JsonSerializer.Serialize(tactical.Employment.DiagnosticDuties);
        Ticks(tactical, 40);
        Check(JsonSerializer.Serialize(tactical.Employment.DiagnosticDuties) == dutyBeforeTurn,
            "input waiting freezes employment alongside other local simulation");
        Check(tactical.UseTrainingEquipment(CombatActionKind.ReadyWeapon), "a paid tactical action may resolve local time");
        Ticks(tactical, 10);
        Check(Duty(tactical, "tech").Phase == DutyPhase.Interrupted && Duty(tactical).Phase == DutyPhase.OnDuty,
            "paid tactical ticks reserve participants while nonparticipant staff retain real duties");

        var recoveryDefinition = definition with
        {
            Home = new(9, -1), Trolley = new(9, -2),
            Starts = [new("avatar", new(10, 0), 5), new("observer", new(5, 0), 1), new("tech", new(8, -2), 1)],
            Employment = employment with
            {
                Posts = [employment.Posts[0] with { LookAt = new Hex(9, -1).World }],
                Appointments = [employment.Appointments[0], employment.Appointments[1] with { Station = new(8, -2), Rest = new(12, -2) }]
            }
        };
        var recoveryConfig = JsonSerializer.Serialize(recoveryDefinition);
        var recovery = Sim(recoveryConfig);
        Check(recovery.IssueOrder("tech", PersonnelTask.Transport, recoveryDefinition.Home), "employed technician may accept actual recovery work");
        Ticks(recovery, 300);
        Check(recovery.Operations.OrderHistory.Any(o => o.Task == PersonnelTask.Transport && o.Status == OrderStatus.Complete)
            && Duty(recovery, "tech").Phase == DutyPhase.OnDuty,
            "a physically completed recovery returns its employed handler to scheduling without an infinite follow-up order");
        Check(!recovery.Operations.OrderHistory.Any(o => o.ActorId == "tech" && o.Status is OrderStatus.Active or OrderStatus.Blocked),
            "completion releases the employee reservation instead of silently double-booking them");

        var safe = normal.Save();
        void Reject(Action<JsonNode> tamper)
        {
            var bad = JsonNode.Parse(safe)!; tamper(bad);
            try { normal.Restore(bad.ToJsonString()); throw new Exception("Invalid employment accepted"); }
            catch (ArgumentException) { Check(normal.Save() == safe, "rejected restore must leave all live state intact"); }
        }
        Reject(n => n["Employment"]!["Duties"]![0]!["ActorId"] = "missing");
        Reject(n => n["Employment"]!["Duties"]![0]!["ReliefSinceMs"] = 999999);
        Reject(n => n["Employment"]!["Duties"]![0]!["Phase"] = 999);
        Reject(n => n["Employment"]!["Reports"]![0]!["ActorId"] = "missing");
        Reject(n => n.AsObject().Remove("Employment"));
        try { _ = Sim(Config(first: chen with { ActorId = "avatar" })); throw new Exception("Player autopilot appointment accepted"); }
        catch (ArgumentException) { passed++; }
        try
        {
            _ = Sim(JsonSerializer.Serialize(definition with { Employment = employment with { Appointments = [null!] } }));
            throw new Exception("Null appointment accepted");
        }
        catch (ArgumentException) { passed++; }
        var noEmploymentConfig = JsonSerializer.Serialize(definition with { Employment = null });
        var legacy = new SpatialSimulation(map, noEmploymentConfig);
        Check(legacy.Employment.Definition is null && legacy.Operations.OrderHistory.Count == 2, "scenarios without employment retain the existing explicit-duty fixture");
        var noEmploymentSave = legacy.Save();
        var invalidDuty = JsonNode.Parse(noEmploymentSave)!;
        invalidDuty["Operations"]!["Orders"]![0]!["Task"] = (int)PersonnelTask.ResumeDuty;
        try { legacy.Restore(invalidDuty.ToJsonString()); throw new Exception("Unappointed return-duty restore accepted"); }
        catch (ArgumentException) { Check(legacy.Save() == noEmploymentSave, "restore rejects a return-duty command without a matching appointment"); }
        Console.WriteLine($"EMPLOYMENT_CHECKS_PASS: {passed} assertions; duties, actual relief, bounded overtime, orders, holds, delivery and persistence.");
        return passed;
    }
}
