using System.Collections.Generic;

namespace Burmuruk.RPGStarterTemplate.Missions
{
    public class MissionSecuence : Mission
    {
        List<Mission> missions;
        List<Mission> completed;
        int _CurMission = 0;

        public bool OrderedList { get; set; }

        public void Init()
        {
            foreach (var mission in missions)
            {
                mission.OnStarted += _ => Start();
                mission.OnFinished += _ => Finish();
            }
        }

        public override void Start()
        {
            if (missions[_CurMission].Started) return;

            if (_CurMission == 0)
            {
                base.Start();
            }

            Started = true;
            missions[_CurMission].Start();
        }

        public override void Finish()
        {
            if (missions[_CurMission].Completed) return;

            completed.Add(missions[_CurMission]);
            missions.RemoveAt(0);

            if (missions.Count == 0)
            {
                base.Finish();
                Completed = true;
                return;
            }

            ++_CurMission;
        }
    }
}
