using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TellWarStories
{

    public class TellStories : CampaignBehaviorBase
    {       
        Dictionary<Village, ToldStoriesTo> _villagesToldTo = new Dictionary<Village, ToldStoriesTo>();
        int _notableBattlesWon = 0;
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(OnMapEvent));
        }

        private void game_menu_tellstories_village_on_consequence(MenuCallbackArgs args)
        {
            Village village = Settlement.CurrentSettlement.Village;
            if(!_villagesToldTo.ContainsKey(village))
            {
                _villagesToldTo.Add(village, new ToldStoriesTo());              
            }
            if (_villagesToldTo[village]._hasToldStories == false)
            {
                DoTellStories(_villagesToldTo[village]);
            }
            else if (_villagesToldTo[village]._hasToldStories)
            {
                if (CampaignTime.Now >= _villagesToldTo[village]._daysToResetStories)
                {
                    DoTellStories(_villagesToldTo[village]);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("You have already told war stories to these villagers today, come back on " + _villagesToldTo[village]._daysToResetStories));
                }
            }
        }

        private void OnMapEvent(MapEvent obj)
        {
            switch (obj.BattleState)
            {
                case BattleState.None:
                    break;
                case BattleState.DefenderVictory:
                case BattleState.AttackerVictory:
                    var winnerSide = obj.BattleState == BattleState.AttackerVictory ? obj.AttackerSide : obj.DefenderSide;
                    var winnerParties = winnerSide.PartiesOnThisSide;
                    int enemyAmountWonAgainst = obj.BattleState == BattleState.AttackerVictory ? obj.DefenderSide.Casualties : obj.AttackerSide.Casualties;
                    foreach (var VARIABLE in winnerParties)
                    {
                        if (VARIABLE.Owner == null) continue;
                        if (VARIABLE.Owner == Hero.MainHero)
                        {
                            if (Hero.MainHero.Clan.Tier >= 0 && Hero.MainHero.Clan.Tier < 2)
                            {
                                if (enemyAmountWonAgainst >= 15)
                                {
                                    GainAStory();
                                }
                            }
                            else if (Hero.MainHero.Clan.Tier >= 1 && Hero.MainHero.Clan.Tier < 3)
                            {
                                if (enemyAmountWonAgainst >= 40)
                                {
                                    GainAStory();
                                }
                            }
                            else if (Hero.MainHero.Clan.Tier >= 2)
                            {
                                if (enemyAmountWonAgainst >= 100)
                                {
                                    GainAStory();
                                }
                            }
                        }
                    }
                    break;
                case BattleState.Dispersed:
                    break;
                default:
                    break;
            }
        }

        private void GainAStory()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have gained a story about a notable battle"));
            _notableBattlesWon++;
        }

        private void DoTellStories(ToldStoriesTo village)
        {
            if (village._battleStoriesTold < _notableBattlesWon)
            {
                SkillObject skill;
                int num;
                skill = SkillObject.FindFirst((x) => { return x.StringId == "Charm"; });
                num = (int)Math.Ceiling(MobileParty.MainParty.LeaderHero.GetSkillValue(skill) * 0.03f);
                float _renownToGive = CalculateRenownToGive(num);
                GainRenownAction.Apply(Hero.MainHero, _renownToGive, true);
                if ((double)_renownToGive <= 0.2)
                    {
                        village._daysToResetStories = CampaignTime.DaysFromNow(this.RandomizeDays());
                        village._hasToldStories = true;
                        ++village._battleStoriesTold;
                        Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 1);
                        InformationManager.DisplayMessage(new InformationMessage("Your story failed to inspire the villagers."));
                        return;
                    }
                InformationManager.DisplayMessage(new InformationMessage("You told the villagers a story about a notable battle, gained " + _renownToGive + " renown."));
                village._daysToResetStories = CampaignTime.DaysFromNow(RandomizeDays());
                village._hasToldStories = true;
                village._battleStoriesTold++;
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, MBRandom.RandomInt(1, 3));
                if (_renownToGive >= 2.0)
                {
                    if(Settlement.CurrentSettlement.Notables.Count >= 1)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Notable people in village were impressed by your feats and like you more."));
                        foreach (Hero notablePerson in Settlement.CurrentSettlement.Notables)
                        {
                            int _relationToGive = CalculateRelationToGive(_renownToGive);
                            ChangeRelationAction.ApplyPlayerRelation(notablePerson, _relationToGive, false, true);
                        }
                    }
                }
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have new stories to tell to these villagers."));
            }
        }

        private float CalculateRenownToGive(int num)
        {
            int _rAmount = MBRandom.RandomInt(1, 20);
            InformationManager.DisplayMessage(new InformationMessage("Random Result: " + _rAmount.ToString() + " Charm Skill Bonus: " + num.ToString()));
            _rAmount += num;
            InformationManager.DisplayMessage(new InformationMessage("Total Result: " + _rAmount.ToString()));

            return (float)_rAmount * 0.1f;
        }

        private int CalculateRelationToGive(float renown)
        {
            InformationManager.DisplayMessage(new InformationMessage("Result for Relation: " + renown.ToString()));
            if ((double)renown < 3.0)
                return (int)MBRandom.RandomInt(1, 4);
            else if ((double)renown < 2.4)
                return (int)MBRandom.RandomInt(1, 3);
            else
                return (int)MBRandom.RandomInt(1, 2);

        }


        private float RandomizeDays()
        {
            int _rAmount = MBRandom.RandomInt(1, 4);
            return _rAmount;
        }

        private bool game_menu_tellstories_here_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return true;
        }
        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            AddWarStoryMenu(obj);
        }

        public void AddWarStoryMenu(CampaignGameStarter obj)
        {
            obj.AddGameMenuOption("village", "village_tellstories", "Tell war stories", game_menu_tellstories_here_on_condition, this.game_menu_tellstories_village_on_consequence, false, 3);
        }
        public class ToldStoriesTo
        {
            [SaveableField(1)]
            public bool _hasToldStories = false;
            [SaveableField(2)]
            public CampaignTime _daysToResetStories = CampaignTime.Now;
            [SaveableField(3)]
            public int _battleStoriesTold = 0;

        }
        public class TellWarStoriesSaveDefiner : SaveableTypeDefiner
        {
            public TellWarStoriesSaveDefiner() : base(18401685)
            {
            }

            protected override void DefineClassTypes()
            {
                AddClassDefinition(typeof(ToldStoriesTo), 1);
            }

            protected override void DefineContainerDefinitions()
            {
                ConstructContainerDefinition(typeof(Dictionary<Village, ToldStoriesTo>));
            }
        }
        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData("_villagesToldTo", ref _villagesToldTo);
                dataStore.SyncData("_notableBattlesWon", ref _notableBattlesWon);
            }
            catch (NullReferenceException doesntExist)
            {

            }
        }
    }





}

