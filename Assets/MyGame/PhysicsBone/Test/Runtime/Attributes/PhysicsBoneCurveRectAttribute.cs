using UnityEngine;

namespace TK.PhysicsBone
{
    public class PhysicsBoneCurveRectAttribute : PropertyAttribute
    {
        public Rect rect;
        public Color color = Color.green;

        public PhysicsBoneCurveRectAttribute()
        {
            this.rect = new Rect(0, 0, 1, 1);
        }
        public PhysicsBoneCurveRectAttribute(Rect rect)
        {
            this.rect = rect;
        }
        public PhysicsBoneCurveRectAttribute(float x, float y, float width, float height)
        {
            this.rect = new Rect(x, y, width, height);
        }
        public PhysicsBoneCurveRectAttribute(Rect rect, Color color)
        {
            this.rect = rect;
            this.color = color;
        }
        public PhysicsBoneCurveRectAttribute(float x, float y, float width, float height, Color color)
        {
            this.rect = new Rect(x, y, width, height);
            this.color = color;
        }
    }
    public class PhysicsBoneEditorAttribute : PropertyAttribute
    {
    }
}
