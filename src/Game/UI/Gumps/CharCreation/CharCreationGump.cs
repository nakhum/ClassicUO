﻿#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.Linq;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CharCreationGump : Gump
    {
        private PlayerMobile _character;
        private int _cityIndex;
        private CharCreationStep _currentStep;
        private LoadingGump _loadingGump;
        private readonly LoginScene _loginScene;
        private ProfessionInfo _selectedProfession;


        public CharCreationGump(LoginScene scene) : base(0, 0)
        {
            _loginScene = scene;
            //Add(new CreateCharAppearanceGump(), 1);
            //SetStep(CharCreationStep.Appearence);   
            //CanCloseWithRightClick = false;
            CreateNewChar(_character);
        }

        // NEU - Setzt die default Charakterwerte.
        public void CreateNewChar(PlayerMobile character)
        {
            if (_character == null)
            {
                _character = new PlayerMobile(1);
                World.Mobiles.Add(_character);
            }
            _character.Hue = 0;
            _character.Name = "Neuer Charakter";
            _character.Race = RaceType.HUMAN;
            _character.IsFemale = false;
            _selectedProfession = new ProfessionInfo();
            _selectedProfession.DescriptionIndex = 1;
            SetCharacter(_character);

        }

        internal static int _skillsCount => Client.Version >= ClientVersion.CV_70160 ? 4 : 3;

        public void SetCharacter(PlayerMobile character)
        {

            _character = character;

            //SetStep(CharCreationStep.ChooseProfession);

            //SetStep(CharCreationStep.ChooseCity);
            SetProfession(_selectedProfession);

        }

        public void SetAttributes(bool force = false)
        {
            
            
            SetStep
            (
                _selectedProfession.DescriptionIndex >= 0 || force ?
                    CharCreationStep.ChooseCity :
                    CharCreationStep.ChooseTrade
            );
        }

        public void SetCity(int cityIndex)
        {
            _cityIndex = cityIndex;
        }

        public void SetProfession(ProfessionInfo info)
        {
            for (int i = 0; i < _skillsCount; i++)
            {
                int skillIndex = info.SkillDefVal[i, 0];

                if (skillIndex >= _character.Skills.Length)
                {
                    continue;
                }

                if (!CUOEnviroment.IsOutlands &&
                    (World.ClientFeatures.Flags & CharacterListFlags.CLF_SAMURAI_NINJA) == 0 &&
                    (skillIndex == 52 || skillIndex == 53))
                {
                    // reset skills if needed
                    for (int k = 0; k < i; k++)
                    {
                        Skill skill = _character.Skills[info.SkillDefVal[k, 0]];
                        skill.ValueFixed = 0;
                        skill.BaseFixed = 0;
                        skill.CapFixed = 0;
                        skill.Lock = Lock.Locked;
                    }

                    MessageBoxGump messageBox = new MessageBoxGump
                        (400, 300, ClilocLoader.Instance.GetString(1063016), null, true)
                        {
                            X = 470 / 2 - 400 / 2 + 100,
                            Y = 372 / 2 - 300 / 2 + 20,
                            CanMove = false
                        };

                    UIManager.Add(messageBox);

                    return;
                }

                Skill skill2 = _character.Skills[skillIndex];
                skill2.ValueFixed = (ushort) info.SkillDefVal[i, 1];
                skill2.BaseFixed = 0;
                skill2.CapFixed = 0;
                skill2.Lock = Lock.Locked;
            }

            _selectedProfession = info;
            _character.Strength = (ushort) _selectedProfession.StatsVal[0];
            _character.Intelligence = (ushort) _selectedProfession.StatsVal[1];
            _character.Dexterity = (ushort) _selectedProfession.StatsVal[2];

            SetAttributes();

            SetStep
                (_selectedProfession.DescriptionIndex > 0 ? CharCreationStep.ChooseCity : CharCreationStep.ChooseTrade);
        }

        public void CreateCharacter(byte profession)
        {
            
            _loginScene.CreateCharacter(_character, _cityIndex, profession);
        }

        public void StepBack(int steps = 1)
        {
            if (_currentStep == CharCreationStep.Appearence)
            {
                _loginScene.StepBack();
            }
            else
            {
                SetStep(_currentStep - steps);
            }
        }

        public void ShowMessage(string message)
        {
            int currentPage = ActivePage;

            if (_loadingGump != null)
            {
                Remove(_loadingGump);
            }

            Add(_loadingGump = new LoadingGump(message, LoginButtons.OK, a => ChangePage(currentPage)), 4);
            ChangePage(4);
        }

        private void SetStep(CharCreationStep step)
        {
            _currentStep = step;

            switch (step)
            {
                default:
                case CharCreationStep.Appearence:
                    ChangePage(1);

                    break;

                case CharCreationStep.ChooseProfession:
                    Control existing = Children.FirstOrDefault(page => page.Page == 2);

                    if (existing != null)
                    {
                        Remove(existing);
                    }

                    Add(new CreateCharProfessionGump(), 2);

                    ChangePage(2);

                    break;

                case CharCreationStep.ChooseTrade:
                    existing = Children.FirstOrDefault(page => page.Page == 3);

                    if (existing != null)
                    {
                        Remove(existing);
                    }

                    Add(new CreateCharTradeGump(_character, _selectedProfession), 3);
                    ChangePage(3);

                    break;

                case CharCreationStep.ChooseCity:

                    CreateCharacter((byte)_selectedProfession.DescriptionIndex);
                    /*existing = Children.FirstOrDefault(page => page.Page == 4);

                    if (existing != null)
                    {
                        Remove(existing);
                    }

                    Add(new CreateCharSelectionCityGump((byte) _selectedProfession.DescriptionIndex, _loginScene), 4);

                    ChangePage(4);*/

                    break;
            }
        }

        private enum CharCreationStep
        {
            Appearence = 0,
            ChooseProfession = 1,
            ChooseTrade = 2,
            ChooseCity = 3
        }
    }
}