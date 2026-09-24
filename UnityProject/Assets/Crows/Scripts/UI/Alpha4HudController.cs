using System;
using DungeonsCrows.Online;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DungeonsCrows.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class Alpha4HudController : MonoBehaviour
    {
        [SerializeField] private OnlineCampaignController campaign;

        private UIDocument _document;
        private VisualElement _root;
        private Label _statusLabel;
        private Label _chapterLabel;
        private Label _enemyLabel;
        private ProgressBar _enemyHealth;
        private Label _playerHealthText;
        private VisualElement _playerHealthFill;
        private Label _objectiveLabel;
        private Label _scoreLabel;
        private Label _relicLabel;
        private Label _turnLabel;
        private TextField _speechField;
        private Button _attackButton;
        private Button _defendButton;
        private Button _interactButton;
        private Button _speakButton;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (campaign == null)
                campaign = FindFirstObjectByType<OnlineCampaignController>();
        }

        private void OnEnable()
        {
            if (campaign != null)
            {
                campaign.ProtocolReadyChanged += OnProtocolReady;
                campaign.BusyChanged += OnBusyChanged;
                campaign.SessionChanged += OnSessionChanged;
                campaign.ErrorRaised += OnError;
            }

            Build();
        }

        private void OnDisable()
        {
            if (campaign == null) return;
            campaign.ProtocolReadyChanged -= OnProtocolReady;
            campaign.BusyChanged -= OnBusyChanged;
            campaign.SessionChanged -= OnSessionChanged;
            campaign.ErrorRaised -= OnError;
        }

        private void Update()
        {
            if (campaign == null ||
                campaign.Session == null ||
                campaign.Busy ||
                Keyboard.current == null ||
                IsTypingSpeech())
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame &&
                IsEnabled(_attackButton))
            {
                campaign.Attack();
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame &&
                     IsEnabled(_defendButton))
            {
                campaign.Defend();
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame &&
                     IsEnabled(_interactButton))
            {
                campaign.Interact();
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame &&
                     IsEnabled(_speakButton))
            {
                campaign.Speak(_speechField?.value ?? string.Empty);
            }
        }

        public void SetCampaign(OnlineCampaignController controller)
        {
            if (campaign == controller) return;

            if (isActiveAndEnabled && campaign != null)
            {
                campaign.ProtocolReadyChanged -= OnProtocolReady;
                campaign.BusyChanged -= OnBusyChanged;
                campaign.SessionChanged -= OnSessionChanged;
                campaign.ErrorRaised -= OnError;
            }

            campaign = controller;

            if (isActiveAndEnabled && campaign != null)
            {
                campaign.ProtocolReadyChanged += OnProtocolReady;
                campaign.BusyChanged += OnBusyChanged;
                campaign.SessionChanged += OnSessionChanged;
                campaign.ErrorRaised += OnError;
            }

            Build();
        }

        private void Build()
        {
            if (_document == null)
                _document = GetComponent<UIDocument>();

            _root = _document.rootVisualElement;
            _root.Clear();
            _root.style.flexGrow = 1;
            _root.style.color = new Color(0.92f, 0.88f, 0.8f);

            if (campaign == null || campaign.Session == null)
                BuildGate();
            else
                BuildGameHud();

            Refresh();
        }

        private void BuildGate()
        {
            VisualElement veil = new VisualElement();
            veil.style.position = Position.Absolute;
            veil.style.left = 0;
            veil.style.right = 0;
            veil.style.top = 0;
            veil.style.bottom = 0;
            veil.style.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 0.88f);
            veil.style.alignItems = Align.FlexStart;
            veil.style.justifyContent = Justify.Center;
            veil.style.paddingLeft = 48;
            _root.Add(veil);

            Label title = new Label("DUNGEONS & CROWS");
            title.style.fontSize = 42;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            veil.Add(title);

            Label subtitle = new Label("SOVEREIGN SHARD · ALPHA 4 CLIENT");
            subtitle.style.fontSize = 11;
            subtitle.style.letterSpacing = 3;
            subtitle.style.color = new Color(0.72f, 0.57f, 0.38f);
            subtitle.style.marginBottom = 22;
            veil.Add(subtitle);

            TextField nameField = new TextField("Adventurer");
            nameField.maxLength = OnlineProtocol.MaxPlayerNameLength;
            nameField.style.width = 320;
            nameField.value = "Crowmant";
            veil.Add(nameField);

            TextField codeField = new TextField("Hunt code");
            codeField.maxLength = 6;
            codeField.style.width = 320;
            codeField.style.marginTop = 8;
            veil.Add(codeField);

            VisualElement buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 14;
            veil.Add(buttons);

            Button create = new Button(() => campaign?.CreateHunt(nameField.value))
            {
                text = "NEW HUNT"
            };
            StyleButton(create);
            buttons.Add(create);

            Button join = new Button(() =>
                campaign?.JoinHunt(
                    (codeField.value ?? string.Empty).ToUpperInvariant(),
                    nameField.value))
            {
                text = "JOIN HUNT"
            };
            StyleButton(join);
            join.style.marginLeft = 8;
            buttons.Add(join);

            _statusLabel = new Label(
                campaign != null && campaign.ProtocolReady
                    ? "Online protocol ready."
                    : "Verifying dc-turn/2.0…");
            _statusLabel.style.marginTop = 16;
            _statusLabel.style.color = new Color(0.7f, 0.72f, 0.68f);
            veil.Add(_statusLabel);
        }

        private void BuildGameHud()
        {
            BuildTopBar();
            BuildPlayerOrb();
            BuildObjectiveTracker();
            BuildHotbar();
        }

        private void BuildTopBar()
        {
            VisualElement top = new VisualElement();
            top.style.position = Position.Absolute;
            top.style.left = 24;
            top.style.right = 24;
            top.style.top = 20;
            top.style.height = 88;
            top.style.flexDirection = FlexDirection.Row;
            top.style.justifyContent = Justify.SpaceBetween;
            _root.Add(top);

            _chapterLabel = new Label();
            _chapterLabel.style.fontSize = 14;
            _chapterLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _chapterLabel.style.color = new Color(0.85f, 0.7f, 0.48f);
            top.Add(_chapterLabel);

            VisualElement enemyBox = new VisualElement();
            enemyBox.style.width = 410;
            enemyBox.style.alignItems = Align.Center;
            top.Add(enemyBox);

            _enemyLabel = new Label();
            _enemyLabel.style.fontSize = 13;
            _enemyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            enemyBox.Add(_enemyLabel);

            _enemyHealth = new ProgressBar();
            _enemyHealth.style.width = 380;
            _enemyHealth.style.marginTop = 5;
            enemyBox.Add(_enemyHealth);

            VisualElement rightBox = new VisualElement();
            rightBox.style.width = 180;
            rightBox.style.alignItems = Align.FlexEnd;
            top.Add(rightBox);

            _turnLabel = new Label();
            _turnLabel.style.width = 180;
            _turnLabel.style.unityTextAlign = TextAnchor.UpperRight;
            rightBox.Add(_turnLabel);

            Button leave = new Button(() => campaign?.LeaveHunt())
            {
                text = "LEAVE HUNT"
            };
            StyleButton(leave);
            leave.style.width = 120;
            leave.style.marginTop = 6;
            rightBox.Add(leave);
        }

        private void BuildPlayerOrb()
        {
            VisualElement orb = new VisualElement();
            orb.style.position = Position.Absolute;
            orb.style.left = 28;
            orb.style.bottom = 26;
            orb.style.width = 124;
            orb.style.height = 124;
            orb.style.borderTopLeftRadius = 62;
            orb.style.borderTopRightRadius = 62;
            orb.style.borderBottomLeftRadius = 62;
            orb.style.borderBottomRightRadius = 62;
            orb.style.backgroundColor = new Color(0.08f, 0.02f, 0.025f, 0.92f);
            orb.style.borderLeftWidth = 3;
            orb.style.borderRightWidth = 3;
            orb.style.borderTopWidth = 3;
            orb.style.borderBottomWidth = 3;
            orb.style.borderLeftColor = new Color(0.38f, 0.28f, 0.2f);
            orb.style.borderRightColor = new Color(0.38f, 0.28f, 0.2f);
            orb.style.borderTopColor = new Color(0.38f, 0.28f, 0.2f);
            orb.style.borderBottomColor = new Color(0.38f, 0.28f, 0.2f);
            orb.style.overflow = Overflow.Hidden;
            _root.Add(orb);

            _playerHealthFill = new VisualElement();
            _playerHealthFill.style.position = Position.Absolute;
            _playerHealthFill.style.left = 0;
            _playerHealthFill.style.right = 0;
            _playerHealthFill.style.bottom = 0;
            _playerHealthFill.style.height = 100;
            _playerHealthFill.style.backgroundColor =
                new Color(0.52f, 0.035f, 0.045f, 0.95f);
            orb.Add(_playerHealthFill);

            _playerHealthText = new Label();
            _playerHealthText.style.position = Position.Absolute;
            _playerHealthText.style.left = 0;
            _playerHealthText.style.right = 0;
            _playerHealthText.style.top = 48;
            _playerHealthText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _playerHealthText.style.unityFontStyleAndWeight = FontStyle.Bold;
            orb.Add(_playerHealthText);
        }

        private void BuildObjectiveTracker()
        {
            VisualElement tracker = new VisualElement();
            tracker.style.position = Position.Absolute;
            tracker.style.right = 28;
            tracker.style.top = 120;
            tracker.style.width = 280;
            tracker.style.paddingTop = 12;
            tracker.style.paddingBottom = 12;
            tracker.style.paddingLeft = 14;
            tracker.style.paddingRight = 14;
            tracker.style.backgroundColor = new Color(0.02f, 0.025f, 0.03f, 0.78f);
            _root.Add(tracker);

            Label heading = new Label("ACTIVE OATH");
            heading.style.fontSize = 10;
            heading.style.letterSpacing = 2;
            heading.style.color = new Color(0.75f, 0.58f, 0.36f);
            tracker.Add(heading);

            _objectiveLabel = new Label();
            _objectiveLabel.style.marginTop = 7;
            _objectiveLabel.style.whiteSpace = WhiteSpace.Normal;
            tracker.Add(_objectiveLabel);

            _scoreLabel = new Label();
            _scoreLabel.style.marginTop = 12;
            tracker.Add(_scoreLabel);

            _relicLabel = new Label();
            _relicLabel.style.marginTop = 4;
            _relicLabel.style.whiteSpace = WhiteSpace.Normal;
            tracker.Add(_relicLabel);
        }

        private void BuildHotbar()
        {
            VisualElement dock = new VisualElement();
            dock.style.position = Position.Absolute;
            dock.style.left = new Length(50, LengthUnit.Percent);
            dock.style.bottom = 20;
            dock.style.width = 560;
            dock.style.marginLeft = -280;
            dock.style.alignItems = Align.Center;
            _root.Add(dock);

            VisualElement actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            dock.Add(actions);

            _attackButton = MakeAction("1", "ATTACK", () => campaign?.Attack());
            actions.Add(_attackButton);

            _defendButton = MakeAction("2", "DEFEND", () => campaign?.Defend());
            actions.Add(_defendButton);

            _interactButton = MakeAction("3", "RITE", () => campaign?.Interact());
            actions.Add(_interactButton);

            _speakButton = MakeAction("4", "SPEAK", () =>
                campaign?.Speak(_speechField?.value ?? string.Empty));
            actions.Add(_speakButton);

            _speechField = new TextField();
            _speechField.maxLength = OnlineProtocol.MaxFreeformTextLength;
            _speechField.style.width = 420;
            _speechField.style.marginTop = 7;
            _speechField.value = string.Empty;
            dock.Add(_speechField);

            _statusLabel = new Label();
            _statusLabel.style.marginTop = 5;
            _statusLabel.style.fontSize = 10;
            _statusLabel.style.color = new Color(0.7f, 0.72f, 0.68f);
            dock.Add(_statusLabel);
        }

        private Button MakeAction(string key, string label, Action action)
        {
            Button button = new Button(action)
            {
                text = key + "\n" + label
            };
            StyleButton(button);
            button.style.width = 106;
            button.style.height = 58;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            return button;
        }

        private static void StyleButton(Button button)
        {
            button.style.backgroundColor = new Color(0.045f, 0.05f, 0.06f, 0.96f);
            button.style.color = new Color(0.93f, 0.87f, 0.75f);
            button.style.borderLeftColor = new Color(0.42f, 0.3f, 0.18f);
            button.style.borderRightColor = new Color(0.42f, 0.3f, 0.18f);
            button.style.borderTopColor = new Color(0.42f, 0.3f, 0.18f);
            button.style.borderBottomColor = new Color(0.42f, 0.3f, 0.18f);
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        private void OnProtocolReady(bool ready)
        {
            if (_statusLabel != null)
                _statusLabel.text = ready
                    ? "dc-turn/2.0 ready"
                    : "Protocol unavailable";
            Refresh();
        }

        private void OnBusyChanged(bool busy)
        {
            Refresh();
        }

        private void OnSessionChanged(GameSessionDto session)
        {
            bool wantsGate = session == null;
            bool isGate = _chapterLabel == null;
            if (wantsGate != isGate)
                Build();
            else
                Refresh();
        }

        private void OnError(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
                _statusLabel.style.color = new Color(0.95f, 0.46f, 0.38f);
            }
        }

        private void Refresh()
        {
            if (campaign == null) return;
            GameSessionDto session = campaign.Session;
            if (session == null)
            {
                if (_statusLabel != null && campaign.ProtocolReady)
                    _statusLabel.text = campaign.Busy
                        ? "Contacting the shard…"
                        : "dc-turn/2.0 ready";
                return;
            }

            CombatantDto player = FindPlayer(session, campaign.PlayerId);
            bool active = string.Equals(session.campaignStatus, "active", StringComparison.Ordinal);
            bool ownTurn = active &&
                string.Equals(session.activeActorId, campaign.PlayerId, StringComparison.Ordinal);

            if (_chapterLabel != null)
                _chapterLabel.text = "CHAPTER " + session.chapter + " · " + ChapterName(session.chapter).ToUpperInvariant();

            if (_enemyLabel != null)
                _enemyLabel.text = session.enemy != null
                    ? session.enemy.name.ToUpperInvariant()
                    : "NO ENEMY";

            if (_enemyHealth != null && session.enemy != null)
            {
                _enemyHealth.lowValue = 0;
                _enemyHealth.highValue = Mathf.Max(1, session.enemy.maxHp);
                _enemyHealth.value = session.enemy.hp;
                _enemyHealth.title = session.enemy.hp + " / " + session.enemy.maxHp;
            }

            if (_playerHealthText != null && player != null)
                _playerHealthText.text = player.hp + "\nRESOLVE";

            if (_playerHealthFill != null && player != null)
            {
                float fraction = player.maxHp > 0
                    ? Mathf.Clamp01((float)player.hp / player.maxHp)
                    : 0f;
                _playerHealthFill.style.height = new Length(
                    fraction * 100f,
                    LengthUnit.Percent);
            }

            if (_objectiveLabel != null)
                _objectiveLabel.text = ObjectiveText(session);

            if (_scoreLabel != null)
                _scoreLabel.text = "Score " + session.score + " · Turn " + session.turnNumber;

            if (_relicLabel != null)
                _relicLabel.text = session.relics != null && session.relics.Length > 0
                    ? "Relics: " + string.Join(" · ", session.relics)
                    : "Relics: none";

            if (_turnLabel != null)
                _turnLabel.text = active
                    ? (ownTurn ? "YOUR TURN" : "ALLY TURN")
                    : session.campaignStatus.ToUpperInvariant();

            bool canAct = ownTurn && !campaign.Busy;
            _attackButton?.SetEnabled(canAct && session.enemy != null && session.enemy.hp > 0);
            _defendButton?.SetEnabled(canAct);
            _interactButton?.SetEnabled(canAct && !ObjectiveComplete(session));
            _speakButton?.SetEnabled(canAct);

            if (_statusLabel != null)
            {
                _statusLabel.style.color = new Color(0.7f, 0.72f, 0.68f);
                _statusLabel.text = campaign.Busy
                    ? "Resolving canonical turn…"
                    : (ownTurn ? "The crow turns toward you." : "Waiting for the current oath.");
            }
        }

        private bool IsTypingSpeech()
        {
            if (_speechField == null ||
                _speechField.panel == null ||
                _speechField.panel.focusController == null)
            {
                return false;
            }

            Focusable focused =
                _speechField.panel.focusController.focusedElement;

            return ReferenceEquals(focused, _speechField);
        }

        private static bool IsEnabled(Button button)
        {
            return button != null && button.enabledSelf;
        }

        private static CombatantDto FindPlayer(GameSessionDto session, string playerId)
        {
            if (session?.party == null) return null;
            foreach (CombatantDto member in session.party)
            {
                if (member != null &&
                    string.Equals(member.id, playerId, StringComparison.Ordinal))
                    return member;
            }
            return null;
        }

        private static bool ObjectiveComplete(GameSessionDto session)
        {
            if (session.chapter >= 3) return session.crownBroken;
            if (session.chapter == 2) return session.rookeryPurified;
            return session.altarOpened;
        }

        private static string ObjectiveText(GameSessionDto session)
        {
            if (session.chapter >= 3)
                return session.crownBroken
                    ? "Break the Black Rook Crown · COMPLETE"
                    : "Break the Black Rook Crown · Rite " + session.ritualAttempts + "/3";

            if (session.chapter == 2)
                return session.rookeryPurified
                    ? "Cleanse the Bone Rookery · COMPLETE"
                    : "Cleanse the Bone Rookery · Rite " + session.ritualAttempts + "/3";

            return session.altarOpened
                ? "Awaken the Crow Altar · COMPLETE"
                : "Awaken the Crow Altar";
        }

        private static string ChapterName(int chapter)
        {
            if (chapter >= 3) return "The Black Rook";
            if (chapter == 2) return "The Bone Rookery";
            return "The Crow Crypt";
        }
    }
}
