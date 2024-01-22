using System;
#if UNITY_5_3_OR_NEWER
// using UnityEngine;
#elif GODOT
using Godot;
#endif
namespace System.Collections.Generic{
    public sealed class LinkedStackNode<T>{
        public T Value;
        public readonly LinkedStackNode<T> Next;
        public LinkedStackNode(T value, LinkedStackNode<T> next){
            Value = value;
            Next = next;
// #if UNITY_5_3_OR_NEWER
//             Debug.Log
// #elif GODOT
//             GD.Print
// #else
//             Console.WriteLine
// #endif
//             ("a new node");
        }
        // ~LinkedStackNode(){
        //     #if UNITY_5_3_OR_NEWER
        //     Debug.Log
        //     #elif GODOT
        //     GD.Print
        //     #else
        //     Console.WriteLine
        //     #endif
        //     ("remove a node");
        // }
        [Obsolete("not enabled", true)] private LinkedStackNode(){}
    }
    public class LinkedStack<T>{
        public LinkedStackNode<T> Top{ get; private set; }
        public ulong Count{ get; private set; }
        private static readonly Exception exception
            = new OverflowException();
        public LinkedStack(){
            Count = 0;
            Top = null;
        }
        public void Push(T value){
            if(Count == ulong.MaxValue){
                throw exception;
                // return;
            }
            Top = new LinkedStackNode<T>(value, Top);
            Count++;
        }
        public bool TryPop(){
            if(Top == null) return false;
            Top = Top.Next;
            Count--;
            return true;
        }
        public void Clear(){
            while(Top != null)
                Top = Top.Next;
            Count = 0;
        }
    }
}