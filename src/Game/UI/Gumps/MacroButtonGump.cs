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

using System;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MacroButtonGump : AnchorableGump
    {
        private Texture2D backgroundTexture;
        private Label label;

        public MacroButtonGump(Macro macro, int x, int y) : this()
        {
            X = x;
            Y = y;
            _macro = macro;
            BuildGump();
        }

        public MacroButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }

        public override GumpType GumpType => GumpType.MacroButton;
        public Macro _macro;

        private void BuildGump()
        {
            Width = 88;
            Height = 44;

            label = new Label(_macro.Name, true, 0x03b2, Width, 255, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 0,
                Width = Width - 10
            };

            label.Y = (Height >> 1) - (label.Height >> 1);
            Add(label);

            backgroundTexture = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));
        }

        protected override void OnMouseEnter(int x, int y)
        {
            label.Hue = 53;
            backgroundTexture = SolidColorTextureCache.GetTexture(Color.DimGray);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            label.Hue = 0x03b2;
            backgroundTexture = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));
            base.OnMouseExit(x, y);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            Point offset = Mouse.LDragOffset;

            if (ProfileManager.CurrentProfile.CastSpellsByOneClick && button == MouseButtonType.Left && !Keyboard.Alt &&
                Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                RunMacro();
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (ProfileManager.CurrentProfile.CastSpellsByOneClick || button != MouseButtonType.Left)
            {
                return false;
            }

            RunMacro();

            return true;
        }

        private void RunMacro()
        {
            if (_macro != null)
            {
                GameScene gs = Client.Game.GetScene<GameScene>();
                gs.Macros.SetMacroToExecute(_macro.Items as MacroObject);
                gs.Macros.WaitForTargetTimer = 0;
                gs.Macros.Update();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            HueVector.Z = 0.1f;

            batcher.Draw2D(backgroundTexture, x, y, Width, Height, ref HueVector);

            HueVector.Z = 0;
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Gray), x, y, Width, Height, ref HueVector);

            base.Draw(batcher, x, y);

            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            if (_macro != null)
            {
                // hack to give macro buttons a unique id for use in anchor groups
                int macroid = Client.Game.GetScene<GameScene>().Macros.GetAllMacros().IndexOf(_macro);

                LocalSerial = (uint) macroid + 1000;

                base.Save(writer);

                writer.WriteAttributeString("name", _macro.Name);
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            Macro macro = Client.Game.GetScene<GameScene>().Macros.FindMacro(xml.GetAttribute("name"));

            if (macro != null)
            {
                _macro = macro;
                BuildGump();
            }
        }
    }
}