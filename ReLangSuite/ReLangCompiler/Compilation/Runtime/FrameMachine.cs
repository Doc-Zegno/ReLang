using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Handmada.ReLang.Compilation.Runtime {
    class Frame {
        private Stack<List<IDisposable>> disposableStack;
        private List<IDisposable> disposables;
        private Stack<int> framePointerStack;
        private int framePointer;

        public List<object> Variables { get; }


        public Frame() {
            Variables = new List<object>();
            disposableStack = new Stack<List<IDisposable>>();
            disposables = new List<IDisposable>();
            framePointerStack = new Stack<int>();
            framePointer = 0;
        }


        public void CreateVariable(object value, bool isDisposable) {
            if (framePointer < Variables.Count) {
                Variables[framePointer] = value;
            } else {
                Variables.Add(value);
            }
            framePointer++;

            if (isDisposable) {
                disposables.Add((IDisposable)value);
            }
        }


        public void EnterScope() {
            framePointerStack.Push(framePointer);

            disposableStack.Push(disposables);
            disposables = new List<IDisposable>();
        }


        public void LeaveScope() {
            framePointer = framePointerStack.Pop();

            foreach (var disposable in disposables) {
                disposable.Dispose();
            }
            disposables = disposableStack.Pop();
        }
    }


    class FrameMachine {
        private Stack<Frame> frames;
        private Frame topFrame;


        public FrameMachine() {
            frames = new Stack<Frame>();
            topFrame = null;
        }


        public void EnterFrame() {
            frames.Push(topFrame);
            topFrame = new Frame();
        }


        public void EnterScope() {
            topFrame.EnterScope();
        }


        public void LeaveFrame() {
            topFrame = frames.Pop();
        }


        public void LeaveScope() {
            topFrame.LeaveScope();
        }


        public void CreateVariable(object value, bool isDisposable) {
            topFrame.CreateVariable(value, isDisposable);
        }


        public object GetVariable(int number) {
            return topFrame.Variables[number];
        }


        public void SetVariable(int number, object value) {
            topFrame.Variables[number] = value;
        }
    }
}
