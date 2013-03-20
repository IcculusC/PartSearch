using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace PartSearch
{
    static class I
    {
        private static GameObject _gameObject;

        public static T AddI<T>(string name) where T : Component
        {
            if (_gameObject == null)
            {
                _gameObject = new GameObject(name, typeof(T));
                GameObject.DontDestroyOnLoad(_gameObject);

                return _gameObject.GetComponent<T>();
            }
            else
            {
                if (_gameObject.GetComponent<T>() != null)
                    return _gameObject.GetComponent<T>();
                else
                    return _gameObject.AddComponent<T>();
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

        private List<AvailablePart> _holder = new List<AvailablePart>();
        private Dictionary<AvailablePart, PartCategories> _master = null;

        private TextWithListener _text;

        private GameScenes _scene;

        private PartCategories _panel;

        private bool _categorize;

        private static Texture2D back;

        public void Start()
        {
            if (_text == null) // create a text box with listener, see below
            {
                _text = new TextWithListener(1, "");
                _text.TextChangedListener = Search; // set the boxes callback to the Search function below
            }           

            _scene = HighLogic.LoadedScene;

            byte[] bit = Properties.Resources.eraser_small; // get button image from resources

            back = new Texture2D(25, 25); // make new texture
            back.LoadImage(bit); // load image into texture
        }

        public void Update()
        {
            if (_master == null && PartLoader.LoadedPartsList != null && HighLogic.LoadedSceneIsEditor) // master part list not initialized
            {
                _master = new Dictionary<AvailablePart, PartCategories>(); // create master part list
                PartLoader.LoadedPartsList.ForEach(x => _master.Add(x, x.category)); // populate master part list, the value is the category of the part
            }
        }

        public void OnGUI()
        {
            if (_scene != HighLogic.LoadedScene && _text != null && HighLogic.LoadedSceneIsEditor) // scene changed and there's text in the box and the new scene is the editor
                    _text.setText("");

            if (!EditorLogic.editorLocked && HighLogic.LoadedSceneIsEditor)
            {
                GUI.skin = EditorLogic.fetch.shipBrowserSkin;// AssetBase.GetGUISkin("OrbitMapSkin");

                if (EditorPartList.Instance.categorySelected != _panel) // user clicked new panel
                {
                    if (_text != null)
                        _text.Fire(); // redo the search

                    _panel = EditorPartList.Instance.categorySelected;
                }              

                _text.Draw(); // draw our textbox, see below

                if (GUI.Button(new Rect(_text.getPosition().x, _text.getPosition().y - 26, 25, 25), new GUIContent(back)))// "<"))// new GUIContent(back))) // clear button
                    _text.setText("");

                bool test = _categorize;

                _categorize = GUI.Toggle(new Rect(_text.getPosition().x + 26, _text.getPosition().y - 26, 25, 25), _categorize, "∞", GUI.skin.button);// "✱", GUI.skin.button); // show all categories on the same page?

                if (_categorize != test) // if category button changes, fire the listener
                    _text.Fire();
                
                GUI.skin = HighLogic.Skin;
            }

            _scene = HighLogic.LoadedScene; // refresh _scene to current scene
        }


         /********************************************************
          * This function is called when the user types in the
          * textbox or Fire() is called on _text - our instance
          * of TextWithListener(see below)
         ********************************************************/
        public void Search(int id, string text, int oldlength, int newlength)
        {
            PartLoader.LoadedPartsList.Clear();

            foreach (AvailablePart part in _master.Keys) // reset part categories to original categories
                part.category = _master[part];

            if (text.Contains("[") && text.Contains("]")) // check for search syntax [token]term
            {
                string token = text.Substring(text.IndexOf("[") + 1, text.IndexOf("]") - 1); // get the token, between the [ and ]
                Debug.Log(token);
                string term = text.Remove(0, text.IndexOf("]") + 1); // get the term, everything after ]
                Debug.Log(term);

                switch (token.ToLower()) // search based on the token, if the token isn't one of the following, do the default
                {
                    case "module":
                        PartLoader.LoadedPartsList.AddRange(_master.Keys.Where(x => x.moduleInfo.ToLower().Contains(term.ToLower())));
                        if(_categorize)    
                            PartLoader.LoadedPartsList.ForEach(x => x.category = EditorPartList.Instance.categorySelected);
                        break;
                    case "author":
                        PartLoader.LoadedPartsList.AddRange(_master.Keys.Where(x => x.author.ToLower().Contains(term.ToLower())));
                        if (_categorize)
                            PartLoader.LoadedPartsList.ForEach(x => x.category = EditorPartList.Instance.categorySelected);
                        break;
                    case "description":
                        PartLoader.LoadedPartsList.AddRange(_master.Keys.Where(x => x.description.ToLower().Contains(term.ToLower())));
                        if (_categorize)
                            PartLoader.LoadedPartsList.ForEach(x => x.category = EditorPartList.Instance.categorySelected);
                        break;
                    case "manufacturer":
                        PartLoader.LoadedPartsList.AddRange(_master.Keys.Where(x => x.manufacturer.ToLower().Contains(term.ToLower())));
                        if (_categorize)
                            PartLoader.LoadedPartsList.ForEach(x => x.category = EditorPartList.Instance.categorySelected);
                        break;
                    default:
                        PartLoader.LoadedPartsList.ForEach(x => x.category = EditorPartList.Instance.categorySelected);
                        if (_categorize)
                            PartLoader.LoadedPartsList.AddRange(_master.Keys.Where(x => x.title.ToLower().Contains(text.ToLower()) || x.partPrefab.GetType().FullName.Contains(text.ToLower()) || x.moduleInfo.ToLower().Contains(text.ToLower())));
                        break;
                }
            }
            else if (text != "") // no tokens, but text in the box
            {
                PartLoader.LoadedPartsList.AddRange(_master.Keys.Where(x => x.title.ToLower().Contains(text.ToLower()) || x.partPrefab.GetType().FullName.Contains(text.ToLower()) || x.moduleInfo.ToLower().Contains(text.ToLower())));
                if (_categorize)
                    PartLoader.LoadedPartsList.ForEach(x => x.category = EditorPartList.Instance.categorySelected);
            }
            else // reset the part list
                PartLoader.LoadedPartsList.AddRange(_master.Keys);

            EditorPartList.Instance.Refresh(); // refresh the editor
            PartLoader.LoadedPartsList.Clear(); // clear the part list
            PartLoader.LoadedPartsList.AddRange(_master.Keys); // reset the part list
        }
    }

    class TextWithListener
    {
        public delegate void TextChanged(int id, string text, int oldlength, int newlength); // text change delegate definition

        public TextChanged TextChangedListener = null; // instance's delegate

        private string _lastText = "";
        private string _currentText = "";

        private Rect pos;

        private int _id = 0;

        public TextWithListener(int id, string startText)
        {
            _currentText = startText;
            _id = id;
        }

        public void Draw()
        {
            if (_currentText != _lastText) // text changed
            {
                if (TextChangedListener != null)
                    TextChangedListener(_id, _currentText, _lastText.Length, _currentText.Length); // fire the listener

                _lastText = _currentText;
            }

            pos = new Rect(EditorPanels.Instance.partsPanelWidth + 10, Screen.height - EditorPanels.Instance.partsPanelWidth / 3 - 8, 100, 25); // calculate position and store it for getPosition()

            _currentText = GUI.TextField(pos, _currentText, GUI.skin.textField);
        }

        public void Fire() // manually fire the listener by calling this function
        {
            if (TextChangedListener != null)
                TextChangedListener(_id, _currentText, _lastText.Length, _currentText.Length);
        }

        public void setText(string text)
        {
            _currentText = text;
        }

        public Rect getPosition()
        {
            return pos;
        }
    }
}
