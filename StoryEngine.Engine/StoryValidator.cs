using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using global::StoryEngine.Model;
using StoryEngine.Model;

namespace StoryEngine.Engine
{

    public static class StoryValidator
    {
        public record ValidationError(string Category, string Message);

        public static List<ValidationError> Validate(
            StoryDefinition story,
            ICollection<string> loadedImageNames)
        {
            var errors = new List<ValidationError>();
            var blockIds = new HashSet<string>(StringComparer.Ordinal);
            var propKeys = new HashSet<string>(StringComparer.Ordinal);

            // ── 1. Titlu ───────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(story.Title))
                errors.Add(new("Structură", "Titlul poveștii este gol."));

            // ── 2. StartBlock definit ──────────────────────────────
            if (string.IsNullOrWhiteSpace(story.StartBlock))
                errors.Add(new("Structură", "Blocul de start nu este definit."));

            // ── 3. ID-uri unice de bloc ────────────────────────────
            foreach (var block in story.Blocks)
            {
                if (string.IsNullOrWhiteSpace(block.Id))
                { errors.Add(new("Blocuri", "Există un bloc fără ID.")); continue; }

                if (!blockIds.Add(block.Id))
                    errors.Add(new("Blocuri", $"ID duplicat: '{block.Id}'"));
            }

            // ── 4. StartBlock există ───────────────────────────────
            if (!string.IsNullOrWhiteSpace(story.StartBlock)
                && !blockIds.Contains(story.StartBlock))
                errors.Add(new("Structură",
                    $"Blocul de start '{story.StartBlock}' nu există în poveste."));

            // ── 5. Proprietăți ─────────────────────────────────────
            foreach (var prop in story.Properties)
            {
                if (string.IsNullOrWhiteSpace(prop.Key))
                { errors.Add(new("Proprietăți", "Există o proprietate fără cheie.")); continue; }

                if (!propKeys.Add(prop.Key))
                    errors.Add(new("Proprietăți", $"Cheie duplicată: '{prop.Key}'"));

                if (prop.Min > prop.Max)
                    errors.Add(new("Proprietăți",
                        $"'{prop.Key}': min ({prop.Min}) > max ({prop.Max})"));

                if (prop.Initial < prop.Min || prop.Initial > prop.Max)
                    errors.Add(new("Proprietăți",
                        $"'{prop.Key}': initial ({prop.Initial}) nu e în [{prop.Min}, {prop.Max}]"));

                if (!string.IsNullOrWhiteSpace(prop.OnMinBlock)
                    && !blockIds.Contains(prop.OnMinBlock))
                    errors.Add(new("Proprietăți",
                        $"'{prop.Key}': onMinBlock '{prop.OnMinBlock}' nu există."));

                if (!string.IsNullOrWhiteSpace(prop.OnMaxBlock)
                    && !blockIds.Contains(prop.OnMaxBlock))
                    errors.Add(new("Proprietăți",
                        $"'{prop.Key}': onMaxBlock '{prop.OnMaxBlock}' nu există."));
            }

            // ── 6. Blocuri – decizii, efecte, condiții, imagini ────
            foreach (var block in story.Blocks)
            {
                if (!string.IsNullOrWhiteSpace(block.BackgroundImage)
                    && !loadedImageNames.Contains(block.BackgroundImage))
                    errors.Add(new("Imagini",
                        $"Blocul '{block.Id}': imaginea '{block.BackgroundImage}' nu e în arhivă."));

                foreach (var dec in block.Decisions)
                {
                    string ctx = $"Bloc '{block.Id}', decizie '{dec.Text}'";

                    if (string.IsNullOrWhiteSpace(dec.TargetBlock))
                        errors.Add(new("Decizii", $"{ctx}: lipsă bloc destinație."));
                    else if (!blockIds.Contains(dec.TargetBlock))
                        errors.Add(new("Decizii",
                            $"{ctx}: destinație '{dec.TargetBlock}' nu există."));

                    foreach (var eff in dec.Effects)
                        if (!propKeys.Contains(eff.Property))
                            errors.Add(new("Efecte",
                                $"{ctx}: efect pe proprietate inexistentă '{eff.Property}'."));

                    CheckConditionProps(ctx, dec.Condition, propKeys, errors);
                }
            }

            return errors;
        }

        private static void CheckConditionProps(
            string ctx, ConditionNode? node,
            HashSet<string> propKeys, List<ValidationError> errors)
        {
            if (node == null) return;
            if (node.Type == "COMPARISON" && node.Property != null
                && !propKeys.Contains(node.Property))
                errors.Add(new("Condiții",
                    $"{ctx}: condiție pe proprietate inexistentă '{node.Property}'."));
            if (node.Conditions != null)
                foreach (var child in node.Conditions)
                    CheckConditionProps(ctx, child, propKeys, errors);
        }
    }
}
