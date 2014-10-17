using System;
using System.Collections.Generic;
using System.Linq;
using KSP.IO;
using UnityEngine;

namespace PartSearch
{
    static class I
    {
        private static GameObject gameObject;

        public static T AddI<T>(string name) where T : Component
        {
            if (gameObject == null)
            {
                gameObject = new GameObject(name, typeof(T));
                GameObject.DontDestroyOnLoad(gameObject);

                return gameObject.GetComponent<T>();
            }
            else
            {
                return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
            }
        }
    }

    public class PartSearch : KSP.Testing.UnitTest
    {
        public PartSearch() : base()
        {
            I.AddI<PSBehaviour>("PSImmortal");
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class PSBehaviour : MonoBehaviour
    {

        /*
         * Things to note:
         * 
         * The editor decides what to display based on PartLoader.LoadedPartList
         * 
         * Calling EditorPartList.Instance.Refresh() manually updates the editorfrom the above source
         * 
         * EditorPartList.Instance.categorySelected determines which category the user currently has up
         */

        private const String WND_POSITION = "PartSearchWndPos";

        private Dictionary<AvailablePart, PartCategories> master = null;
        private TextWithListener text;
        private GameScenes gameScene;
        private PartCategories panel;
        private bool categorize = true;
        //private static Texture2D back;
        private Rect wndRect = new Rect(400, 40, 300, 65);
		private string key = "space";
		private string keyMod = "left ctrl";
        private ApplicationLauncherButton tbButton;
        private Boolean showGUI = false;
        private bool setFocus = true;
        private PluginConfiguration config;
        private EditorPartListFilter filter;

        public void Start()
        {
            if (text == null) // create a searchText box with listener, see below
            {
                text = new TextWithListener(1, "") { 
                    textChangedListener = (id, s, oldlength, newlength) => 
                    {
                        EditorPartList.Instance.SelectTab(EditorPartList.Instance.categorySelected); // Selects the first page of the current category
                        EditorPartList.Instance.Refresh();
                    }
                };
            }

            gameScene = HighLogic.LoadedScene;

            config = PluginConfiguration.CreateForType<PartSearch>();
            config.load();
            wndRect = config.GetValue(WND_POSITION, new Rect(400, 40, 300, 65));
			key = config.GetValue("PartSearchKey", "space");
			keyMod = config.GetValue("PartSearchKeyMod", "left ctrl");

            filter = new EditorPartListFilter("PartSearchFilter", p => Search(p, text.GetText()));

            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            //byte[] bit = Properties.Resources.eraser_small; // get button image from resources

            //back = new Texture2D(25, 25); // make new texture
            //back.LoadImage(bit); // load image into texture
        }

        public void Awake() {
            RenderingManager.AddToPostDrawQueue(0, OnDraw);
        }

        public void Update()
        {
            if (master == null && PartLoader.LoadedPartsList != null && HighLogic.LoadedSceneIsEditor) // master part list not initialized
            {
                master = new Dictionary<AvailablePart, PartCategories>(); // create master part list
                PartLoader.LoadedPartsList.ForEach(x => master.Add(x, x.category)); // populate master part list, the value is the category of the part
            }
        }

        void OnGUIAppLauncherReady() {
            if (ApplicationLauncher.Ready && tbButton == null) {
                tbButton = ApplicationLauncher.Instance.
                    AddModApplication(OnToggleOn, OnToggleOff, null, null, null, null, 
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
                    GameDatabase.Instance.GetTexture("PartSearch/Textures/tbbutton", false));
            }
        }


        void OnGameSceneLoadRequested(GameScenes scene) {
            if (tbButton != null) {
                Destroy();
            }
        }

        private void OnToggleOn() {
            DisableCategories(categorize);
            EditorPartList.Instance.ExcludeFilters.AddFilter(filter);
            showGUI = true;
            setFocus = true;
        }

        private void OnToggleOff() {
            DisableCategories(false);
            text.SetText("");
            text.Fire();
            showGUI = false;
            EditorPartList.Instance.ExcludeFilters.RemoveFilter(filter);
        }

        public void OnDraw()
        {
			if (Input.GetKey(keyMod) && Input.GetKeyDown(key)) {
				if(showGUI) setFocus=true;
				else tbButton.SetTrue();
			}

            if (gameScene != HighLogic.LoadedScene && text != null && HighLogic.LoadedSceneIsEditor) { // scene changed and there's searchText in the box and the new scene is the editor {
                    text.SetText("");
            }

            if (showGUI && !EditorLogic.editorLocked && HighLogic.LoadedSceneIsEditor && EditorLogic.fetch)
            {
                GUI.skin = EditorLogic.fetch.shipBrowserSkin;// AssetBase.GetGUISkin("OrbitMapSkin");

                if (EditorPartList.Instance.categorySelected != (int) panel) // user clicked new panel
                {
                    if (text != null) {
                        text.Fire();
                    }
                    panel = (PartCategories)EditorPartList.Instance.categorySelected;
                }

                wndRect = GUILayout.Window("Hello".GetHashCode(), wndRect, id => {
                    GUILayout.BeginHorizontal();
                    text.Draw(); // draw our textbox, see below
                    if (setFocus) {
                        GUI.FocusControl("PartSearchTextbox");
                        setFocus = false;
                    }

                    if (GUILayout.Button(new GUIContent("C", "Clear search"))) {
                        text.SetText("");
                    }

                    bool test = categorize;

                    categorize = GUILayout.Toggle(categorize,new GUIContent("∞", "Search all categories"), GUI.skin.button);
                    GUILayout.EndHorizontal();

                    if (categorize != test) {// if category button changes, fire the listener
                        DisableCategories(categorize);
                        text.Fire();
                    }

                    GUI.DragWindow();
                }, "Search");


                GUI.skin = HighLogic.Skin;
            }

            gameScene = HighLogic.LoadedScene; // refresh _scene to current scene
        }

        public void OnDestroy() {
            Destroy();
        }

        private void Destroy() {
            DisableCategories(false);
            config.SetValue(WND_POSITION, wndRect);
			config.SetValue("PartSearchKey", key);
			config.SetValue("PartSearchKeyMod", keyMod);
            config.save();

            if (tbButton != null) {
                ApplicationLauncher.Instance.RemoveModApplication(tbButton);
                tbButton = null;
            }

            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
        }

        private void DisableCategories(bool disable) {
            if (disable) {
                EditorPartList.Instance.HideTabs();
                PartLoader.LoadedPartsList.ForEach(
                    x => x.category = (PartCategories)EditorPartList.Instance.categorySelected);
            } else {
                EditorPartList.Instance.ShowTabs();
                PartLoader.LoadedPartsList.ForEach(
                    x => x.category = master[x]);
            }
            EditorPartList.Instance.SelectTab(EditorPartList.Instance.categorySelected); // Selects the first page of the current category
            EditorPartList.Instance.Refresh();
        }

        private bool Search(AvailablePart part, string searchText) {
            string[] words = searchText.Split(" ".ToCharArray());

            bool ret = false;
            foreach (string word in words) {
                ret |= part.title.ToLower().Contains(word.ToLower()) || 
                    part.partPrefab.GetType().FullName.Contains(word.ToLower()) || 
                    part.moduleInfo.ToLower().Contains(word.ToLower()) || 
                    part.description.ToLower().Contains(word.ToLower());
            }

            return ret;
        }
    }


    public class TextWithListener {
        public delegate void TextChanged(int id, string text, int oldlength, int newlength); // searchText change delegate definition

        public TextChanged textChangedListener = null; // instance's delegate

        private string lastText = "";
        private string currentText = "";

        private Rect pos;

        private int _id = 0;

        public TextWithListener(int id, string startText)
        {
            currentText = startText;
            _id = id;
        }

        public void Draw()
        {
            if (currentText != lastText) // searchText changed
            {
                if (textChangedListener != null)
                    textChangedListener(_id, currentText, lastText.Length, currentText.Length); // fire the listener

                lastText = currentText;
            }

            pos = new Rect(EditorPanels.Instance.partsPanelWidth + 10, Screen.height - EditorPanels.Instance.partsPanelWidth / 3 - 8, 100, 25); // calculate position and store it for getPosition()

            GUI.SetNextControlName("PartSearchTextbox");
            currentText = GUILayout.TextField(currentText, GUI.skin.textField, GUILayout.MinWidth(200));
        }

        public void Fire() // manually fire the listener by calling this function
        {
            if (textChangedListener != null)
                textChangedListener(_id, currentText, lastText.Length, currentText.Length);
        }

        public void SetText(string text)
        {
            currentText = text;
        }

        public String GetText() {
            return currentText;
        }

        public Rect GetPosition()
        {
            return pos;
        }
    }
}
