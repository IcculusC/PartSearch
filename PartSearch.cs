using System;
using System.Collections.Generic;
using System.Linq;
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

        private Dictionary<AvailablePart, PartCategories> master = null;

        private TextWithListener text;

        private GameScenes scene;

        private PartCategories panel;

        private bool categorize = true;

        private static Texture2D back;

        public void Start()
        {
            if (text == null) // create a searchText box with listener, see below
            {
                text = new TextWithListener(1, "") {textChangedListener = Search};
            }

            scene = HighLogic.LoadedScene;

            byte[] bit = Properties.Resources.eraser_small; // get button image from resources

            back = new Texture2D(25, 25); // make new texture
            back.LoadImage(bit); // load image into texture
        }

        public void Update()
        {
            if (master == null && PartLoader.LoadedPartsList != null && HighLogic.LoadedSceneIsEditor) // master part list not initialized
            {
                master = new Dictionary<AvailablePart, PartCategories>(); // create master part list
                PartLoader.LoadedPartsList.ForEach(x => master.Add(x, x.category)); // populate master part list, the value is the category of the part
            }
        }

        public void OnGUI()
        {
            if (scene != HighLogic.LoadedScene && text != null && HighLogic.LoadedSceneIsEditor) { // scene changed and there's searchText in the box and the new scene is the editor {
                    text.SetText("");
            }

            if (!EditorLogic.editorLocked && HighLogic.LoadedSceneIsEditor && EditorLogic.fetch)
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

                


                text.Draw(); // draw our textbox, see below

                if (GUI.Button(new Rect(text.GetPosition().x, text.GetPosition().y - 26, 25, 25), new GUIContent(back))) // "<"))// new GUIContent(back))) // clear button
                {
                    text.SetText("");
                }

                bool test = categorize;

                categorize = GUI.Toggle(new Rect(text.GetPosition().x + 26, text.GetPosition().y - 26, 25, 25), categorize, "∞", GUI.skin.button);// "✱", GUI.skin.button); // show all categories on the same page?

                if (categorize != test) // if category button changes, fire the listener
                {
                    text.Fire();
                }
                
                GUI.skin = HighLogic.Skin;

                if (Input.GetKeyUp(KeyCode.Escape))  //Clear Textbox
                {
                    text.SetText("");
                    text.Fire(); // redo the search
                }
                else if (Input.GetKeyUp(KeyCode.Slash))
                {
                    GUI.FocusControl("PartSearchTextbox");
                }
            }

            scene = HighLogic.LoadedScene; // refresh _scene to current scene
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

    class TextWithListener
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
            currentText = GUI.TextField(pos, currentText, GUI.skin.textField);
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
