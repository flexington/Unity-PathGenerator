using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace flexington.PathGenerator
{
    public class PathGeneratorComponent : MonoBehaviour
    {
        [SerializeField, HideInInspector] private Path _path;
        public Path Path
        {
            get { return _path; }
            set { _path = value; }
        }

        [SerializeField, HideInInspector] private bool _isClosed;
        public bool IsClosed
        {
            get { return _isClosed; }
            set { _isClosed = value; }
        }



        public void GeneratePath()
        {
            _path = new Path(transform.position - new Vector3(.5f, 0, 0), transform.position + new Vector3(.5f, 0, 0));
        }
    }
}


