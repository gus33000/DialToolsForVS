﻿using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Windows.UI.Input;
using System.Windows.Forms;
using System.Drawing;

namespace DialToolsForVS
{
    internal class ColorController : IDialController
    {
        private DTE2 _dte = VsHelpers.DTE;
        private IWpfTextView _view;
        private Span _span;

        public Specificity Specificity => Specificity.CaretPosition;

        public bool CanHandleClick => false;

        public bool CanHandleRotate
        {
            get
            {
                if (_dte.ActiveWindow?.Kind != "Document")
                    return false;

                _view = VsHelpers.GetCurentTextView();

                SnapshotPoint position = _view.Caret.Position.BufferPosition;
                IWpfTextViewLine line = _view.GetTextViewLineContainingBufferPosition(position);

                if (line.Extent.IsEmpty)
                    return false;

                MatchCollection matches = Regex.Matches(line.Extent.GetText(), @"(#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3}))\b");

                foreach (Match match in matches)
                {
                    var matchSpan = new Span(line.Start + match.Index, match.Length);

                    if (matchSpan.Start <= position && matchSpan.End >= position)
                    {
                        _span = matchSpan;
                        return true;
                    }
                }

                return false;
            }
        }

        public bool OnClick(RadialControllerButtonClickedEventArgs args)
        {
            throw new NotImplementedException();
        }

        public bool OnRotate(RotationDirection direction)
        {
            if (_view == null || _span == null || _span.IsEmpty)
                return false;

            var bufferSpan = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, _span);
            string text = bufferSpan.GetText();

            int argb = int.Parse(text.Substring(1), NumberStyles.HexNumber);
            var color = Color.FromArgb(argb);

            double factor = direction == RotationDirection.Left ? -3 : 3;

            Color newColor = AdjustBrightness(color, factor);

            UpdateSpan(bufferSpan, ConvertToHex(newColor).ToLowerInvariant());

            return true;
        }

        private static string ConvertToHex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        public Color AdjustBrightness(Color color, double darkenAmount)
        {
            var hslColor = new HSLColor(color);
            hslColor.Luminosity += darkenAmount; // 0 to 1
            return hslColor;
        }

        private static void UpdateSpan(SnapshotSpan span, string result)
        {
            if (result.Length > 1)
                result = result.TrimStart('0');

            using (ITextEdit edit = span.Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(span, result);
                edit.Apply();
            }
        }
    }
}
