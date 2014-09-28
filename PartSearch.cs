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
        private static Texture2D back;
        private Rect wndRect = new Rect(400, 40, 300, 65);
        private ApplicationLauncherButton tbButton;
        private Boolean showGUI = false;
        private PluginConfiguration config;

        public void Start()
        {
            if (text == null) // create a searchText box with listener, see below
            {
                text = new TextWithListener(1, "") { textChangedListener = Search };
            }

            gameScene = HighLogic.LoadedScene;

            config = KSP.IO.PluginConfiguration.CreateForType<PartSearch>();
            config.load();
            wndRect = config.GetValue(WND_POSITION, new Rect(400, 40, 300, 65));

            //byte[] bit = Properties.Resources.eraser_small; // get button image from resources

            //back = new Texture2D(25, 25); // make new texture
            //back.LoadImage(bit); // load image into texture
        }

        public void Awake() {
            RenderingManager.AddToPostDrawQueue(0, OnDraw);
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
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
            if (ApplicationLauncher.Ready) {
                tbButton = ApplicationLauncher.Instance.
                    AddModApplication(OnToggleOn, OnToggleOff, null, null, null, null, 
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
                    GameDatabase.Instance.GetTexture("PartSearch/Textures/tbbutton", false));
            }
        }


        void OnGameSceneLoadRequested(GameScenes scene) {
            if (tbButton != null) {
                ApplicationLauncher.Instance.RemoveModApplication(tbButton);
                tbButton = null;
            }
        }

        private void OnToggleOn() {
            showGUI = true;
        }

        private void OnToggleOff() {
            text.SetText("");
            text.Fire();
            showGUI = false;
        }

        public void OnDraw()
        {
            if (gameScene != HighLogic.LoadedScene && text != null && HighLogic.LoadedSceneIsEditor) { // scene changed and there's searchText in the box and the new scene is the editor {
                    text.SetText("");
            }

            if (showGUI && !EditorLogic.editorLocked && HighLogic.LoadedSceneIsEditor && EditorLogic.fetch)
            {
                GUI.skin = EditorLogic.fetch.shipBrowserSkin;// AssetBase.GetGUISkin("OrbitMapSkin");

                if (EditorPartList.Instance.categorySelected != (int) panel) // user clicked new panel
                {
                    if (text != null)
                    {
                        if (categorize)
                        {
                            text.SetText("");
                        }
                        else
                        {
                            text.Fire();
                        }
                    }
                    panel = (PartCategories)EditorPartList.Instance.categorySelected;
                }

                wndRect = GUILayout.Window("Hello".GetHashCode(), wndRect, id => {
                    GUILayout.BeginHorizontal();
                    text.Draw(); // draw our textbox, see below

                    if (GUILayout.Button(new GUIContent("C", "Clear search"))) {
                        text.SetText("");
                    }

                    bool test = categorize;

                    categorize = GUILayout.Toggle(categorize,new GUIContent("∞", "Search all categories"), GUI.skin.button);
                    GUILayout.EndHorizontal();

                    if (categorize != test) {// if category button changes, fire the listener
                        text.Fire();
                    }

                    GUI.DragWindow();
                }, "Search");


                GUI.skin = HighLogic.Skin;

                if (Input.GetKeyUp(KeyCode.Escape))  //Clear Textbox
                {
                    text.SetText("");
                    text.Fire(); // redo the search
                } else if (Input.GetKeyUp(KeyCode.Slash)) {
                    GUI.FocusControl("PartSearchTextbox");
                }
            }

            gameScene = HighLogic.LoadedScene; // refresh _scene to current scene
        }

        public void OnDestroy() {
            config.SetValue(WND_POSITION, wndRect);
            config.save();

            if (tbButton != null) {
                ApplicationLauncher.Instance.RemoveModApplication(tbButton);
                tbButton = null;
            }

            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
        }


         /********************************************************
          * This function is called when the user types in the
          * textbox or Fire() is called on _text - our instance
          * of TextWithListener(see below)
         ********************************************************/
        public void Search(int id, string searchText, int oldlength, int newlength)
        {
            PartLoader.LoadedPartsList.Clear();

            foreach (AvailablePart part in master.Keys) // reset part categories to original categories
            {
                part.category = master[part];
            }

            if (searchText.Contains("[") && searchText.Contains("]")) // check for search syntax [token]term
            {
                string token = searchText.Substring(searchText.IndexOf("[") + 1, searchText.IndexOf("]") - 1); // get the token, between the [ and ]
                Debug.Log(token);
                string term = searchText.Remove(0, searchText.IndexOf("]") + 1); // get the term, everything after ]
                Debug.Log(term);

                switch (token.ToLower()) // search based on the token, if the token isn't one of the following, do the default
                {
                    case "module":
                        PartLoader.LoadedPartsList.AddRange(master.Keys.Where(x => x.moduleInfo.ToLower().Contains(term.ToLower())));
                        if(categorize)
                            PartLoader.LoadedPartsList.ForEach(x => x.category = (PartCategories) EditorPartList.Instance.categorySelected);
                        break;
                    case "author":
                        PartLoader.LoadedPartsList.AddRange(master.Keys.Where(x => x.author.ToLower().Contains(term.ToLower())));
                        if (categorize)
                            PartLoader.LoadedPartsList.ForEach(x => x.category = (PartCategories) EditorPartList.Instance.categorySelected);
                        break;
                    case "description":
                        PartLoader.LoadedPartsList.AddRange(master.Keys.Where(x => x.description.ToLower().Contains(term.ToLower())));
                        if (categorize)
                            PartLoader.LoadedPartsList.ForEach(x => x.category = (PartCategories) EditorPartList.Instance.categorySelected);
                        break;
                    case "manufacturer":
                        PartLoader.LoadedPartsList.AddRange(master.Keys.Where(x => x.manufacturer.ToLower().Contains(term.ToLower())));
                        if (categorize)
                            PartLoader.LoadedPartsList.ForEach(x => x.category = (PartCategories) EditorPartList.Instance.categorySelected);
                        break;
                    default:
                        PartLoader.LoadedPartsList.ForEach(x => x.category = (PartCategories) EditorPartList.Instance.categorySelected);
                        if (categorize)
                            PartLoader.LoadedPartsList.AddRange(master.Keys.Where(x => x.title.ToLower().Contains(searchText.ToLower()) || x.partPrefab.GetType().FullName.Contains(searchText.ToLower()) || x.moduleInfo.ToLower().Contains(searchText.ToLower())));
                        break;
                }
            }
            else if (searchText != "") // no tokens, but searchText in the box
            {
                string[] words = searchText.Split(" ".ToCharArray());

                PartLoader.LoadedPartsList.AddRange(master.Keys); //Add all parts to filter

                foreach (string word in words) //Filter pasts on each word
                {
                    List<AvailablePart> tmp = new List<AvailablePart>();
                    tmp.AddRange(PartLoader.LoadedPartsList.Where(x => x.title.ToLower().Contains(word.ToLower()) || x.partPrefab.GetType().FullName.Contains(word.ToLower()) || x.moduleInfo.ToLower().Contains(word.ToLower()) || x.description.ToLower().Contains(word.ToLower())));
                    PartLoader.LoadedPartsList.Clear();
                    PartLoader.LoadedPartsList.AddRange(tmp);
                }
                if (categorize)
                {
                    PartLoader.LoadedPartsList.ForEach(x => x.category = (PartCategories)EditorPartList.Instance.categorySelected);
                }
            }
            else // reset the part list
            {
                PartLoader.LoadedPartsList.AddRange(master.Keys);
            }

            EditorPartList.Instance.Refresh(); // refresh the editor
            PartLoader.LoadedPartsList.Clear(); // clear the part list
            PartLoader.LoadedPartsList.AddRange(master.Keys); // reset the part list
        }
    }

    public class TextWithListener
    {
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

        public Rect GetPosition()
        {
            return pos;
        }
    }
}
