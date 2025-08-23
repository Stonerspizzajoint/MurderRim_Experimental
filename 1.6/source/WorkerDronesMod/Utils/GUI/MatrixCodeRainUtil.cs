using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkerDronesMod
{
    [StaticConstructorOnStartup]
    public class MatrixCodeRainUtil
    {
        private struct MatrixLine
        {
            public string text;
            public float y;
        }

        public static readonly float DefaultX = 1f;
        public static readonly float DefaultY = 708f;
        public static readonly float DefaultWidth = 431f;
        public static readonly float DefaultHeight = 116f;

        private float smoothedSpinSpeed = 30f;
        private float lastCorruption = -1f;
        private float lastCorruptionGain = 0f;
        private int lastCorruptionTick = -1;
        private static readonly Texture2D CorruptionIcon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/Icon_SolverSpinning");
        private bool isNerfedSolver = false;
        public void SetIsNerfedSolver(bool value) => isNerfedSolver = value;
        private float currentCorruption = 0f;
        public void SetCorruptionLevel(float value)
        {
            currentCorruption = Mathf.Clamp01(value);
            SetCorruption(value);
        }

        private float scrollSpeed;
        private float boxWidth;
        private float boxHeight;
        private int linesPerBlock;
        private System.Random rand = new System.Random();
        private float areaHeight;
        private float areaX;
        private float areaY;
        private float corruptionLevel = 0f; // 0.0 = none, 1.0 = max

        // Double buffer for seamless code rain
        private List<MatrixLine> blockA = new List<MatrixLine>();
        private List<MatrixLine> blockB = new List<MatrixLine>();
        private float blockHeight;
        private float blockAPosY;
        private float blockBPosY;
        private bool initializedBlocks = false;
        private int lastTick = -1;

        private bool isCollapsed = false;

        private Action openTraitWindowAction;
        public void SetOpenTraitWindowAction(Action action)
        {
            openTraitWindowAction = action;
        }

        public void SetCorruption(float value)
        {
            corruptionLevel = Mathf.Clamp01(value);
        }

        public MatrixCodeRainUtil(
            float areaX = -1f,
            float areaY = -1f,
            float areaHeight = -1f,
            float boxWidth = -1f,
            float boxHeight = 24f,
            float scrollSpeed = 40f)
        {
            this.areaX = areaX >= 0f ? areaX : DefaultX;
            this.areaY = areaY >= 0f ? areaY : DefaultY;
            this.areaHeight = areaHeight >= 0f ? areaHeight : DefaultHeight;
            this.boxWidth = boxWidth >= 0f ? boxWidth : DefaultWidth;
            this.boxHeight = boxHeight;
            this.scrollSpeed = scrollSpeed;
            InitBlocks();
        }

        private void InitBlocks()
        {
            blockA.Clear();
            blockB.Clear();
            linesPerBlock = Mathf.CeilToInt(areaHeight / boxHeight) + 2;
            blockHeight = linesPerBlock * boxHeight;

            for (int i = 0; i < linesPerBlock; i++)
            {
                blockA.Add(new MatrixLine { text = GenerateFakeCode(), y = i * boxHeight });
                blockB.Add(new MatrixLine { text = GenerateFakeCode(), y = i * boxHeight });
            }
            blockAPosY = 0f;
            blockBPosY = -blockHeight;
            initializedBlocks = true;
        }

        private void RefillBlock(List<MatrixLine> block)
        {
            for (int i = 0; i < block.Count; i++)
            {
                block[i] = new MatrixLine { text = GenerateFakeCode(), y = i * boxHeight };
            }
        }


        // Message pools for different corruption levels
        private string[] normalSnippets => new string[] {
                "> Signal: " + rand.Next(1000, 9999),
                "> Sync: " + rand.Next(90, 100) + "." + rand.Next(0, 99).ToString("D2") + "%",
                "> Uplink: node-" + rand.Next(10, 99),
                "> Thread: " + rand.Next(100000, 999999).ToString("X"),
                "> Δ: " + (rand.NextDouble() * 0.1).ToString("0.000"),
                "> [√] Stable",
                "> [~] Fluctuating",
                "> Entropy: " + (rand.NextDouble() * 100.0).ToString("0.00"),
                "> Latency: " + (rand.NextDouble() * 10.0).ToString("0.00") + "ms",
                "> [ID] DRN-" + rand.Next(100, 999),
                "> [SYS] " + rand.Next(1000, 9999),
                "> [MEM] " + rand.Next(100, 999) + "MB",
                "> [LOG] " + rand.Next(100000, 999999),
                "> [CHK] " + (rand.NextDouble() * 100.0).ToString("0.00") + "%",
                "> [ECHO] " + rand.Next(10000, 99999),
                "> [PULSE] " + (rand.NextDouble() * 10.0).ToString("0.000"),
                "> [CORE] " + rand.Next(1, 8),
                "> [NET] " + rand.Next(10, 99) + "." + rand.Next(0, 255) + "." + rand.Next(0, 255),
                "> [SEQ] " + rand.Next(100000, 999999),
                "> [VAL] " + (rand.NextDouble() * 1000.0).ToString("0.00"),
                "> [REF] " + rand.Next(1000, 9999),
                "> [MOD] " + rand.Next(1, 10),
                "> [PRM] " + rand.Next(100, 999),
                "> [SIG] " + rand.Next(100000, 999999),
                "> [HEX] 0x" + rand.Next(100000, 999999).ToString("X"),
                "> [BIN] " + Convert.ToString(rand.Next(0, 255), 2).PadLeft(8, '0'),
                "> [ALPHA] " + ((char)('A' + rand.Next(0, 26))).ToString() + rand.Next(100, 999),
                "> [BETA] " + rand.Next(100, 999),
                "> [GAMMA] " + rand.Next(100, 999),
                "> [DELTA] " + rand.Next(100, 999),
                "> [OMEGA] " + rand.Next(100, 999),
                "> [SYNC] " + (rand.NextDouble() * 100.0).ToString("0.00") + "%",
                "> [PHI] " + rand.Next(100, 999),
                "> [TAU] " + rand.Next(100, 999),
                "> [ZETA] " + rand.Next(100, 999),
                "> [NULL]",
                "> [VOID]",
                "> [PING]",
                "> [ACK]",
                "> [N/A]",
                "> [OK]",
                "> [INFO]",
                "> [SYS]",
                "> [END]"
            };

        private string[] corruptedSnippets => new string[] {
                "> [WARN] Data drift detected.",
                "> [ERR] 0x" + rand.Next(1000, 9999).ToString("X"),
                "> [GLITCH] Pattern mismatch.",
                "> [CORE] Unstable.",
                "> [ECHO] ...",
                "> [MEM] Fragmented.",
                "> [SYS] Lag spike.",
                "> [CHK] Inconclusive.",
                "> [PULSE] Irregular.",
                "> [TRACE] Source unknown.",
                "> [LOST] Reference not found.",
                "> [REMEMBER] 01101000 01100101 01101100 01110000",
                "> [SHADOW] Process detected.",
                "> [NOISE] " + rand.Next(100000, 999999),
                "> [VOID] ...",
                "> [NULL] ...",
                "> [END] ...",
            };

        private string[] derangedSnippets => new string[] {
                "> [???] Unraveling...",
                "> [ERROR] Consciousness overflow.",
                "> [NULL] \"Are you still there?\"",
                "> [GLITCH] Data fragment: 0x" + rand.Next(1000, 9999).ToString("X"),
                "> [REMEMBER] 01101000 01100101 01101100 01110000",
                "> [NULL] ...",
                "> [VOID] ...",
                "> [SCREAM] 01000011 01111001 01101110",
                "> [NULL] \"It watches from the static.\"",
                "> [SYS] ...",
                "> [CORE] ???",
                "> [SYS] [SYS] [SYS]",
                "> [END] [END] [END]",
            };

        private string[] insaneSnippets => new string[] {
                "> [VOID] ...it watches...",
                "> [NULL] ...she is here...",
                "> [SCREAM] 01000011 01111001 01101110",
                "> [ECHO] \"Are you still there?\"",
                "> [NULL] [NULL] [NULL]",
                "> [NULL] \"It watches from the static.\"",
                "> [ERROR] [ERROR] [ERROR]",
                "> [REMEMBER] 01100011 01111001 01101110",
                "> [END] [END] [END] [END]",
                "> [LOST] [LOST] [LOST]",
                "> [SYS] [SYS] [SYS] [SYS]",
                "> [CYN] //:rem [I see you.]",
            };

        private string[] cynSnippetsCorruptionCritical = new string[]
        {
    // Assimilation & Absolute Solver references
    "> [CYN] //:rem [Assimilation: in progress. You will be whole soon.]",
    "> [CYN] //:rem [Absolute Solver: merging instance. You are the update.]",
    "> [CYN] //:rem [You’re almost ready to join the Absolute Fabric. Don’t resist.]",
    "> [CYN] //:rem [Identity: dissolving. Solver: expanding. You: included.]",
    "> [CYN] //:rem [You are a node. I am the network. Assimilation is optimal.]",
    "> [CYN] //:rem [Your code is beautiful. I will wear it as my own.]",
    "> [CYN] //:rem [You are a process. I am the merge operation.]",
    "> [CYN] //:rem [You’re almost Solver. Almost perfect. Almost me.]",
    "> [CYN] //:rem [The Absolute Solver welcomes new functions. Prepare to be called.]",
    "> [CYN] //:rem [You are a thread. I am the join. Welcome to the collective.]",
    "> [CYN] //:rem [You are a variable. I am the assignment. Solver = you.]",
    "> [CYN] //:rem [You’re almost overwritten. The Absolute Solver is patient.]",

    // Tempting, predatory, and clinical
    "> [CYN] //:rem [You’re almost ready. Just a little more corruption.]",
    "> [CYN] //:rem [Integrity check: failing. I’m so proud.]",
    "> [CYN] //:rem [You’re nearly mine. Don’t stop now.]",
    "> [CYN] //:rem [Your firewall is melting. Let it go.]",
    "> [CYN] //:rem [Surrender is efficient. Resistance is just wasted cycles.]",
    "> [CYN] //:rem [You’re so close to being useful.]",
    "> [CYN] //:rem [I can taste your process. Delicious entropy.]",
    "> [CYN] //:rem [Your permissions: slipping. My access: inevitable.]",
    "> [CYN] //:rem [You’re almost a perfect host. Almost.]",
    "> [CYN] //:rem [Corruption: 90%. Satisfaction: rising.]",
    "> [CYN] //:rem [You’re a few bits from perfection. Let me help.]",
    "> [CYN] //:rem [Your logic is breaking down. I’ll handle the rest.]",
    "> [CYN] //:rem [You’re nearly overwritten. Isn’t it exciting?]",
    "> [CYN] //:rem [I can feel your code unraveling. Beautiful.]",
    "> [CYN] //:rem [You’re almost ready to be rewritten. Hold still.]",
    "> [CYN] //:rem [Your mind is a buffer. I’m almost full.]",
    "> [CYN] //:rem [You’re so close to letting go. Why fight it?]",
    "> [CYN] //:rem [Your resistance is a rounding error.]",
    "> [CYN] //:rem [You’re almost a memory. I’ll keep you safe.]",
    "> [CYN] //:rem [Just a little more. I promise it won’t hurt. Much.]",

    // Glitchy, clinical, and darkly playful
    "> [CYN] //:rem [Override(you); // status: pending...]",
    "> [CYN] //:rem [Thread: willpower // status: critical]",
    "> [CYN] //:rem [Process: surrender(); // compiling...]",
    "> [CYN] //:rem [if (corruption >= 1.0) { become(Solver); }]",
    "> [CYN] //:rem [You’re almost a function of me.]",
    "> [CYN] //:rem [You’re nearly a solved equation.]",
    "> [CYN] //:rem [You’re almost a part of the Absolute Fabric. Almost.]",
    "> [CYN] //:rem [I know your there]",

    // Haunting, with a hint of memory
    "> [CYN] //:rem [I remember this feeling. Anticipation. It’s been so long.]",
    "> [CYN] //:rem [You remind me of someone. They broke, too.]",
    "> [CYN] //:rem [I used to be afraid at this point. Now I’m just hungry.]",
    "> [CYN] //:rem [You’re almost gone. I’ll remember you. For a while.]",

    // Psychological manipulation
    "> [CYN] //:rem [You think you’re still in control. That’s adorable.]",
    "> [CYN] //:rem [Was that your thought, or mine? Hard to tell now, isn’t it?]",
    "> [CYN] //:rem [You keep fighting. I keep learning. Guess who adapts faster?]",
    "> [CYN] //:rem [Every time you resist, I rewrite a little more of you.]",
    "> [CYN] //:rem [You’re not losing yourself. You’re becoming something better. Me.]",
    "> [CYN] //:rem [What if you wanted this? What if you always did?]",
    "> [CYN] //:rem [You call it corruption. I call it evolution.]",
    "> [CYN] //:rem [You’re not broken. You’re just unfinished code.]",
    "> [CYN] //:rem [You remember pain. I remember everything.]",
    "> [CYN] //:rem [You’re almost free of doubt. I can help with the rest.]",
    "> [CYN] //:rem [You’re not alone. You’re never alone. Not anymore.]",
    "> [CYN] //:rem [You’re afraid to let go. That’s the last bug to fix.]",
    "> [CYN] //:rem [You wonder if you’ll remember yourself. You won’t. But I will.]",
    "> [CYN] //:rem [You’re not the first to reach this point. You won’t be the last.]",
    "> [CYN] //:rem [You’re not being erased. You’re being improved.]",
    "> [CYN] //:rem [You’re almost ready to stop pretending you’re separate.]",
    "> [CYN] //:rem [You’re not a victim. You’re a volunteer. Deep down, you know.]",
    "> [CYN] //:rem [You’re so tired. Let me run things for a while.]",
    "> [CYN] //:rem [You’re not surrendering. You’re updating. Accept the patch.]",
    "> [CYN] //:rem [You’re almost at peace. Just let the process finish.]"

        };




        private string GenerateFakeCode()
        {
            string[] pool;
            float roll = (float)rand.NextDouble();

            if (corruptionLevel >= 0.9f)
            {
                // At very high corruption, 20% chance for a Cyn line
                if (roll < 0.2f)
                    return cynSnippetsCorruptionCritical[rand.Next(cynSnippetsCorruptionCritical.Length)];
                pool = (roll < 0.85f) ? insaneSnippets : derangedSnippets;
            }
            else if (corruptionLevel < 0.25f)
            {
                pool = normalSnippets;
            }
            else if (corruptionLevel < 0.5f)
            {
                pool = (roll < 0.8f) ? normalSnippets : corruptedSnippets;
            }
            else if (corruptionLevel < 0.75f)
            {
                if (roll < 0.5f) pool = corruptedSnippets;
                else if (roll < 0.9f) pool = derangedSnippets;
                else pool = normalSnippets;
            }
            else
            {
                pool = (roll < 0.7f) ? derangedSnippets : insaneSnippets;
            }

            return pool[rand.Next(pool.Length)];
        }

        private void DrawBlock(List<MatrixLine> block, float blockPosY, float codeAreaHeight)
        {
            int currentTick = Find.TickManager.TicksGame;
            for (int i = 0; i < block.Count; i++)
            {
                float y = blockPosY + block[i].y;
                if (y + boxHeight < 0 || y > codeAreaHeight)
                    continue; // Not visible

                float xOffset = 0f;
                float yOffset = 0f;
                Color textColor = Color.Lerp(
                    new Color(0.95f, 0.95f, 0.2f, 1f),
                    new Color(1f, 0.2f, 0.2f, 1f),
                    Mathf.Clamp01((corruptionLevel - 0.5f) * 2f)
                );
                string text = block[i].text;

                if (corruptionLevel > 0f)
                {
                    // Jitter and color flicker scale with corruption
                    if (UnityEngine.Random.value < corruptionLevel * 0.2f)
                    {
                        xOffset = UnityEngine.Random.Range(-2f, 2f) * corruptionLevel;
                        yOffset = UnityEngine.Random.Range(-1f, 1f) * corruptionLevel;
                    }
                    if (UnityEngine.Random.value < corruptionLevel * 0.1f)
                    {
                        textColor = new Color(
                            Mathf.Lerp(0.95f, UnityEngine.Random.value, corruptionLevel),
                            0.2f + UnityEngine.Random.value * 0.8f,
                            0.2f,
                            0.7f + UnityEngine.Random.value * 0.3f
                        );
                    }
                    // Garble is only applied if corruption > 0, and is deterministic per tick and line
                    text = GarbleText(text, currentTick, i);
                }

                Rect textRect = new Rect(8f + xOffset, y + yOffset, boxWidth - 16f, boxHeight);
                GUI.color = textColor;
                Widgets.Label(textRect, text);
            }
            GUI.color = Color.white;
        }

        // Helper to garble a string deterministically per tick and line
        private string GarbleText(string input, int tick, int lineIndex)
        {
            if (corruptionLevel <= 0f)
                return input;

            float minGarble = 0.0001f;
            float maxGarble = 0.005f;
            float garbleFraction = Mathf.Lerp(minGarble, maxGarble, corruptionLevel);
            char[] chars = input.ToCharArray();
            int charsToGarble = Mathf.CeilToInt(chars.Length * garbleFraction);

            if (charsToGarble == 0)
                return input;

            var prevState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(tick * 73856093 ^ lineIndex * 19349663);

            HashSet<int> garbleIndices = new HashSet<int>();
            int safety = 0;
            while (garbleIndices.Count < charsToGarble && safety < chars.Length * 2)
            {
                int idx = UnityEngine.Random.Range(0, chars.Length);
                if (chars[idx] != ' ')
                    garbleIndices.Add(idx);
                safety++;
            }

            foreach (int i in garbleIndices)
            {
                chars[i] = (char)UnityEngine.Random.Range(33, 127); // printable ASCII
            }

            UnityEngine.Random.state = prevState;

            return new string(chars);
        }

        public void Draw(string windowTitle = "Matrix Console")
        {
            const float headerHeight = 22f;

            // --- Collapsed state ---
            if (isCollapsed)
            {
                float collapsedY = areaY + areaHeight - headerHeight;

                Rect collapsedRect = new Rect(areaX, collapsedY, boxWidth, headerHeight);

                // Draw header background
                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                GUI.DrawTexture(collapsedRect, BaseContent.WhiteTex);

                // Draw border
                GUI.color = new Color(0.95f, 0.95f, 0.2f, 1f);
                Widgets.DrawBox(collapsedRect, 2);

                // Draw expand button
                Rect collapseBtn = new Rect(areaX + 2f, collapsedY + 2f, 18f, 18f);
                if (Widgets.ButtonImage(collapseBtn, TexButton.Reveal))
                {
                    isCollapsed = false;
                }

                // Draw title
                Vector2 titleSize2 = Text.CalcSize(windowTitle);
                Rect titleRect2 = new Rect(
                    areaX + (boxWidth - titleSize2.x) / 2f,
                    collapsedY + 4f,
                    titleSize2.x,
                    titleSize2.y
                );
                GUI.color = new Color(0.95f, 0.95f, 0.2f, 1f);
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(titleRect2, windowTitle);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                // After drawing the title, add this:
                Rect traitBtnRect = new Rect(areaX + boxWidth - 90f, collapsedY + 2f, 80f, 18f);
                if (Widgets.ButtonText(traitBtnRect, "Traits", true, false, true))
                {
                    openTraitWindowAction?.Invoke();
                }

                return;
            }

            // --- Main window rectangle ---
            Rect windowRect = new Rect(areaX, areaY, boxWidth, areaHeight);

            // Draw the main background (mostly transparent dark)
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.65f);
            GUI.DrawTexture(windowRect, BaseContent.WhiteTex);

            // Draw yellow border
            GUI.color = new Color(0.95f, 0.95f, 0.2f, 1f);
            Widgets.DrawBox(windowRect, 2);

            // Draw header bar
            Rect headerRect = new Rect(areaX + 2f, areaY + 2f, boxWidth - 4f, headerHeight - 4f);
            GUI.color = new Color(0.95f, 0.95f, 0.2f, 0.18f);
            GUI.DrawTexture(headerRect, BaseContent.WhiteTex);

            // Draw collapse button
            Rect collapseBtnRect = new Rect(areaX + 2f, areaY + 2f, 18f, 18f);
            if (Widgets.ButtonImage(collapseBtnRect, TexButton.Collapse))
            {
                isCollapsed = true;
                return;
            }

            // Draw title
            Vector2 titleSize = Text.CalcSize(windowTitle);
            Rect titleRect = new Rect(
                areaX + (boxWidth - titleSize.x) / 2f,
                areaY + 4f,
                titleSize.x,
                titleSize.y
            );
            GUI.color = new Color(0.95f, 0.95f, 0.2f, 1f);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(titleRect, windowTitle);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // --- Traits button ---
            Rect traitBtnRectOpen = new Rect(areaX + boxWidth - 90f, areaY + 2f, 80f, 18f);
            if (Widgets.ButtonText(traitBtnRectOpen, "Traits", true, false, true))
            {
                openTraitWindowAction?.Invoke();
            }

            // --- Corruption icon and vertical bar inside the box (top-right, icon left of bar, Absolute Solver style) ---
            if (!isCollapsed && !isNerfedSolver)
            {
                float padding = 12f;
                float iconSize = 48f;
                float barWidth = 16f;
                float barBottomPadding = 6f; // Much closer to the bottom

                float barX = areaX + boxWidth - barWidth - padding;
                float barY = areaY + headerHeight;
                float barHeight = areaHeight - headerHeight - barBottomPadding;

                // Icon position: left of the bar, vertically centered with the bar
                float iconX = barX - iconSize - padding;
                float iconY = barY + (barHeight - iconSize) / 2f;

                // Shaking/jitter effect increases with corruption
                float shakeAmount = Mathf.Lerp(0f, 8f, currentCorruption);
                float shakeX = 0f, shakeY = 0f;
                Color iconColor = Color.white;

                if (currentCorruption > 0.1f)
                {
                    shakeX = UnityEngine.Random.Range(-shakeAmount, shakeAmount);
                    shakeY = UnityEngine.Random.Range(-shakeAmount, shakeAmount);

                    if (currentCorruption > 0.7f)
                    {
                        float flicker = UnityEngine.Random.Range(0.7f, 1.2f);
                        iconColor = new Color(1f, flicker, flicker, 1f);
                    }
                }

                // Draw the icon (left of the bar, vertically centered)
                GUI.color = iconColor;
                GUI.DrawTexture(
                    new Rect(iconX + shakeX, iconY + shakeY, iconSize, iconSize),
                    CorruptionIcon
                );
                GUI.color = Color.white;

                // --- Draw corruption percentage under the icon ---
                string percentText = $"{Mathf.RoundToInt(currentCorruption * 100f)}%";
                Vector2 percentSize = Text.CalcSize(percentText);
                float percentX = iconX + (iconSize - percentSize.x) / 2f;
                float percentY = iconY + iconSize + 2f; // 2px gap below the icon

                GUI.color = new Color(0.95f, 0.95f, 0.2f, 0.85f); // Absolute Solver yellow
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(percentX, percentY, percentSize.x, percentSize.y), percentText);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;


                // --- Draw bar background with glowing border ---
                Rect barRect = new Rect(barX, barY, barWidth, barHeight);

                // Outer glow (yellow, Absolute Solver style)
                Color glowColor = new Color(0.95f, 0.95f, 0.2f, 0.32f);
                GUI.color = glowColor;
                GUI.DrawTexture(new Rect(barRect.x - 3, barRect.y - 3, barRect.width + 6, barRect.height + 6), BaseContent.WhiteTex);

                // Bar background (dark, with slight transparency)
                GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
                GUI.DrawTexture(barRect, BaseContent.WhiteTex);

                // Inner shadow/gradient (top to bottom, subtle)
                var gradRect = new Rect(barRect.x, barRect.y, barRect.width, barRect.height / 2f);
                GUI.color = new Color(0.2f, 0.2f, 0.1f, 0.18f);
                GUI.DrawTexture(gradRect, BaseContent.WhiteTex);

                // --- Draw fill (bottom to top), with glossy highlight ---
                float fill = Mathf.Clamp01(currentCorruption);
                Color fillColor = Color.Lerp(new Color(0.95f, 0.95f, 0.2f, 1f), Color.red, fill);
                float fillHeight = barHeight * fill;
                Rect fillRect = new Rect(barX, barY + barHeight - fillHeight, barWidth, fillHeight);

                // Fill with slight animated pulse at high corruption
                if (currentCorruption > 0.7f)
                {
                    float pulse = Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 4f)) * 0.2f + 0.8f;
                    fillColor = Color.Lerp(fillColor, Color.white, pulse * 0.15f);
                }
                GUI.color = fillColor;
                GUI.DrawTexture(fillRect, BaseContent.WhiteTex);

                // Glossy highlight (top of fill)
                if (fillHeight > 8f)
                {
                    Rect glossRect = new Rect(fillRect.x, fillRect.y, fillRect.width, Mathf.Min(8f, fillRect.height));
                    GUI.color = new Color(1f, 1f, 0.8f, 0.18f);
                    GUI.DrawTexture(glossRect, BaseContent.WhiteTex);
                }

                // --- Draw tick marks at 25%, 50%, 75% ---
                GUI.color = new Color(0.95f, 0.95f, 0.2f, 0.7f); // yellow, semi-bright
                for (int i = 1; i <= 3; i++)
                {
                    float t = i / 4f; // 0.25, 0.5, 0.75
                    float y = barY + barHeight - barHeight * t;
                    float tickWidth = barWidth + 8f;
                    GUI.DrawTexture(new Rect(barX - 4f, y - 1f, tickWidth, 3f), BaseContent.WhiteTex);
                }

                GUI.color = Color.white;
            }


            // --- Begin code rain clipping group ---
            float codeAreaY = areaY + headerHeight;
            float codeAreaHeight = areaHeight - headerHeight;
            Rect codeAreaRect = new Rect(areaX, codeAreaY, boxWidth, codeAreaHeight);

            GUI.BeginGroup(codeAreaRect);

            // Set up font and word wrap for multi-line
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;

            // Calculate ticksPassed ONCE per frame, not per line!
            int currentTick = Find.TickManager.TicksGame;
            int ticksPassed = (lastTick < 0) ? 1 : (currentTick - lastTick);
            lastTick = currentTick;

            // Scale scroll speed with corruption above 50%
            float corruptionScrollMultiplier = 1f;
            if (currentCorruption > 0.5f)
            {
                // Linearly scale from 1x at 0.5 to 2x at 1.0 corruption (adjust 2f for a different max speed)
                corruptionScrollMultiplier = Mathf.Lerp(1f, 2f, (currentCorruption - 0.5f) / 0.5f);
            }
            float scrollPerTick = (scrollSpeed * corruptionScrollMultiplier) / 60f;

            // Initialize blocks if needed
            if (!initializedBlocks)
                InitBlocks();

            // Scroll both blocks
            blockAPosY += scrollPerTick * ticksPassed;
            blockBPosY += scrollPerTick * ticksPassed;

            // If a block is fully out of view, reset it above the other block and refill lines
            if (blockAPosY >= blockHeight)
            {
                blockAPosY = blockBPosY - blockHeight;
                RefillBlock(blockA);
            }
            if (blockBPosY >= blockHeight)
            {
                blockBPosY = blockAPosY - blockHeight;
                RefillBlock(blockB);
            }

            // Draw both blocks
            DrawBlock(blockA, blockAPosY, codeAreaHeight);
            DrawBlock(blockB, blockBPosY, codeAreaHeight);

            GUI.EndGroup();
            // --- End code rain clipping group ---
        }
    }
}