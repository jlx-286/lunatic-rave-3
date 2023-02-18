public class StackNode<T>{
    public T value;
    public StackNode<T> next;
}
public class LinkedStack<T>{
    public StackNode<T> top;
    public long Count{
        get;
        private set;
    }
    public LinkedStack(){
        Count = 0;
        top = null;
    }
    public T Peek(){
        if(top == null)
            return default(T);
        return top.value;
    }
    public void Push(T value){
        if(top != null){
            StackNode<T> node = new StackNode<T>();
            node.value = value;
            node.next = top;
            top = node;
        }else{
            top = new StackNode<T>();
            top.value = value;
            top.next = null;
        }
        Count++;
    }
    public T Pop(){
        if(top == null)
            return default(T);
        T result = top.value;
        top = top.next;
        Count--;
        return result;
    }
    public void Clear(){
        if(top != null)
            while(top.next != null)
                top = top.next;
        top = null;
        Count = 0;
    }
}