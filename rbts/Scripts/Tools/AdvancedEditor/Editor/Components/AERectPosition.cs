using UnityEngine;

public class AERectPosition
{
    public AERectPosition()
    {
        windowWidth_ = width_ + separator_;
        startXColumn = offsetStartXColumn;
        startY_ = offsetStartY_;
        rect = new Rect(startXColumn, startY_, width_, height_);
    }

    public AERectPosition(int windowWidth)
    {
        windowWidth_ = windowWidth;
        if ((width_ + separator_) >= windowWidth) width_ = windowWidth - (separator_ + 1);
        startXColumn = offsetStartXColumn;
        startY_ = offsetStartY_;
        rect = new Rect(startXColumn, startY_, width_, height_);
    }

    public AERectPosition(int windowWidth, Rect rect)
    {
        windowWidth_ = windowWidth;
        startXColumn = (int)rect.x;
        startY_ = (int)rect.y;
        //offsetStartY = (int)rect.y;
        width_ = (int)rect.width;
        height_ = (int)rect.height;
        this.rect = new Rect(rect); //new Rect(startXColumn, startY, width, height);
    }

    public AERectPosition(int offsetStartXColumn, int offsetStartY_, int startXColumn, int startY, int width, int widthOld, int height, int separator, int windowWidth, int tab, int tabCount)
    {
        this.offsetStartXColumn = offsetStartXColumn;
        this.offsetStartY_ = offsetStartY_;
        windowWidth_ = windowWidth;
        this.startXColumn = startXColumn;
        startY_ = startY;
        width_ = width;
        widthOld_ = widthOld;
        height_ = height;
        separator_ = separator;
        tab_ = tab;
        this.tabCount = tabCount;
        rect = new Rect(startXColumn, startY, width, height);
    }

    private int offsetStartXColumn = 1;
    private int offsetStartY_ = 0;
    private int windowWidth_;
    private int startXColumn;
    private int startY_;
    private int startYMax_ = 1;
    private int width_ = 300; //100
    private int widthOld_ = 300;
    private int height_ = 16; //18
    private int heightOne = 0;
    private int heightNext
    {
        get
        {
            if (heightOne != 0)
            {
                int result = heightOne;
                heightOne = 0;
                return result;
            }
            return height_;
        }

        set
        {
            if (heightOne < value) heightOne = value;
        }
    }

    private int heightNextNotSet
    {
        get
        {
            if (heightOne != 0)
            {
                return heightOne;
            }
            return height_;
        }
    }

    public int height
    {
        get
        {
            return height_;
        }

        set
        {
            height_ = value;
        }
    }

    public int windowWidth
    {
        get
        {
            return windowWidth_;
        }
    }

    public int width
    {
        get
        {
            return width_;
        }

        set
        {
            width_ = value;
        }
    }

    public int widthOld
    {
        get
        {
            return widthOld_;
        }

        set
        {
            widthOld_ = value;
        }
    }

    public int separator
    {
        get
        {
            return separator_;
        }

        set
        {
            separator_ = value;
        }
    }

    public int startY
    {
        get
        {
            return startY_;
        }

        set
        {
            startY_ = value + offsetStartY_;
            rect.yMax += startY;
        }
    }

    public int startYMax
    {
        get
        {
            return startYMax_;
        }
    }

    public int startX
    {
        get
        {
            return startXColumn;
        }

        set
        {
            startXColumn = value + offsetStartXColumn;
        }
    }

    private int offsetStartXOld = 0;

    public int offsetStartX
    {
        get
        {
            return offsetStartXColumn;
        }

        set
        {
            offsetStartXOld = value;
            offsetStartX_ = value;
        }
    }

    private int offsetStartX_
    {
        get
        {
            return offsetStartXColumn;
        }

        set
        {
            startXColumn = value - offsetStartXColumn;
            offsetStartXColumn = value;
        }
    }

    public int offsetStartY
    {
        get
        {
            return offsetStartY_;
        }

        set
        {
            startY_ = value - offsetStartY_;
            offsetStartY_ = value;
        }
    }

    private int separator_ = 3;
    public Rect rect;
    public virtual Rect Next(bool returnCarriage = true)
    {
        if (returnCarriage && startXColumn != offsetStartXColumn) startXColumn = offsetStartXColumn;
        rect = new Rect(startXColumn, startY_ += (heightNext + separator_), width_ - tabCount * tab_, height_);
        //startXColumn += (width + separator);
        if (startYMax_ < startY_) startYMax_ = startY_;
        //startY += height;
        return rect; // new Rect(0, 0, 0, 0); nesting * nestingOffSet
    }

    public virtual Rect NextWidthDefault(bool returnCarriage = true)
    {
        if (returnCarriage && startXColumn != offsetStartXColumn) startXColumn = offsetStartXColumn;
        rect = new Rect(startXColumn, startY_ += (heightNext + separator_), width_ - tabCount * tab_, height_);
        startXColumn += (width_ + separator_);
        if (startYMax_ < startY_) startYMax_ = startY_;
        //startY += height;
        return rect;
    }

    public virtual Rect Next(int width, bool returnCarriage = true)
    {
        if (returnCarriage && startXColumn != offsetStartXColumn) startXColumn = offsetStartXColumn;
        rect = new Rect(startXColumn, startY_ += (heightNext + separator_), width - tabCount * tab_, height_);
        //startXColumn += (width + separator);
        if (widthOld_ != width) widthOld_ = width;
        if (startYMax_ < startY_) startYMax_ = startY_;
        //startY += height;
        return rect;
    }

    public virtual Rect NextRelative(int width, bool returnCarriage = true)
    {
        //if (returnCarriage && startXColumn != 1) startXColumn = 1;
        return new Rect(offsetStartXOld + this.width_ - width, rect.y, width, height_); //offsetStartXColumn 
    }

    public virtual Rect NextRelative(int startX, int width, bool returnCarriage = true)
    {
        //if (returnCarriage && startXColumn != 1) startXColumn = 1;
        return new Rect(startX, rect.y, width, height_); //offsetStartXColumn 
    }

    /*public virtual Rect NextLineExit(int width)
    {
        return new Rect(this.width - width, startY, width, height);
    }*/

    public virtual Rect NextNotSet()
    {
        return new Rect(1, startY_ + (heightNextNotSet + separator_), width_ - tabCount * tab_, height_); //heightNext
    }

    public virtual Rect NextNotSet(int width)
    {
        return new Rect(1, startY_ + (heightNextNotSet + separator_), width - tabCount * tab_, height_);
    }

    /*public virtual Rect NextNotSet(int width, int height)
    {
        return new Rect(1, startY_ + (heightNext + separator_), width, height);
    }*/

    public virtual Rect NextNotSetExit(int width)
    {
        return new Rect(offsetStartXOld + this.width_ - width, startY_ + (height_ + separator_), width, height_); //heightNext //offsetStartXColumn + 
    }

    public virtual Rect NotSetStart(int width, int height, int offset)
    {
        return new Rect(offset + rect.x, startY_, width, height);
    }

    public virtual Rect Next(int width, int height, bool returnCarriage = true)
    {
        if (returnCarriage && startXColumn != offsetStartXColumn) startXColumn = offsetStartXColumn;
        rect = new Rect(startXColumn, startY_ += (heightNext + separator_), width - tabCount * tab_, height);
        heightNext = height;
        //startXColumn += (width + separator);
        if (widthOld_ != width) widthOld_ = width;
        if (startYMax_ < startY_) startYMax_ = startY_;
        //startY += (height + separator);
        //startXColumn += (width + separator);
        return rect;
    }

    public virtual Rect NextLine()
    {
        if ((startXColumn + width_ + separator_) > windowWidth_) Next();
        rect = new Rect(startXColumn, startY_, width_ /*- tabCount * tab_*/, height_);
        startXColumn += (width_ + separator_);
        return rect;
    }

    public virtual Rect NextLine(int width, bool nextLeineCarriage = true)
    {
        if (nextLeineCarriage && (startXColumn + width + separator_) > windowWidth_) Next();
        rect = new Rect(startXColumn, startY_, (offsetStartX_ + width) > windowWidth_ ? windowWidth_ - offsetStartX_ : width /*- tabCount * tab_*/, height_);
        startXColumn += (width + separator_);
        //if ((startXColumn + width + separator_) > this.width) startXColumn -= tabCount * tab_; //temp fix
        if (widthOld_ != width) widthOld_ = width;
        return rect;
    }

    public virtual Rect NextLine(int width, int height)
    {
        if ((startXColumn + width + separator_) > windowWidth_) Next();
        rect = new Rect(startXColumn, startY_, (offsetStartX_ + width) > windowWidth_ ? windowWidth_ - offsetStartX_ : width /*- tabCount * tab_*/, height);
        heightNext = height;
        startXColumn += (width + separator_);
        if (widthOld_ != width) widthOld_ = width;
        //this.height = height;
        return rect;
    }

    public virtual Rect NextLine(int width, int height, int offsetStartY)
    {
        if ((startXColumn + width + separator_) > windowWidth_) Next();
        rect = new Rect(startXColumn, startY_ + offsetStartY, width /*- tabCount * tab_*/, height);
        heightNext = height;
        startXColumn += (width + separator_);
        if (widthOld_ != width) widthOld_ = width;
        //this.height = height;
        return rect;
    }

    public virtual Rect PreviousLine(int width, bool nextLeineCarriage = true)
    {
        //if (nextLeineCarriage && (startXColumn + width + separator_) > windowWidth_) Next();
        rect = new Rect(startXColumn, startY_, width - tabCount * tab_, height_);
        startXColumn -= (width + separator_);
        //if ((startXColumn + width + separator_) > this.width) startXColumn -= tabCount * tab_; //temp fix
        if (widthOld_ != width) widthOld_ = width;
        return rect;
    }

    public virtual Rect NextColumn(int width, int height, int startY)
    {
        startY_ = startY;
        if (widthOld_ > 0) startXColumn += (widthOld_ + separator_);
        if ((startXColumn) > windowWidth_) Next();
        rect = new Rect(startXColumn, startY_, width - tabCount * tab_, height);
        heightNext = height;
        if (widthOld_ != width) widthOld_ = width;
        //this.height = height;
        return rect;
    }

    public virtual void NextColumnEnd()
    {
        startY_ = startYMax_;
    }

    public virtual Rect NotSetWidthFixTab(int height)
    {
        return new Rect(startXColumn, startY_, width - tabCount * tab_, height);
        /*if ((startXColumn + width + separator_) > windowWidth_)
        {
            return new Rect(1, startY_ + height + separator_, width - tabCount * tab_, height);
        }
        else
        {
            return new Rect(startXColumn, startY_, width - tabCount * tab_, height);
        }*/
    }

    public virtual Rect NextLineNotSet(int width, int height)
    {
        //if ((startXColumn + width + separator) > windowWidth)
        if ((startXColumn + width + separator_) > windowWidth_)
        {
            return new Rect(1, startY_ + height + separator_, width - tabCount * tab_, height);
        }
        else
        {
            return new Rect(startXColumn, startY_, width - tabCount * tab_, height);
        }
    }

    public virtual Rect GetRect()
    {
        return rect;
    }

    public virtual Rect GetRect(int width, int height)
    {
        rect.width = width;
        rect.height = height;
        heightNext = height;
        return rect;
    }

    public virtual Rect GetRect(int height)
    {
        rect.height = height;
        heightNext = height;
        return rect;
    }

    public virtual void Clear()
    {
        heightOne = 0; //heightNext;
    }

    public virtual void UpdateRect()
    {
        rect = new Rect(startXColumn, startY, width - tabCount * tab_, height_);
    }

    public virtual void UpdateRect(int width, int height)
    {
        rect = new Rect(startXColumn, startY, width - tabCount * tab_, height);
    }

    /*public virtual Rect GetRect(int startXColumn, int countLines)
    {
        //return new Rect(startXColumn, startY, width, height * countLines);
        return rect;
    }*/

    public virtual bool CheckPositionColumn(AERectPosition rectPosition)
    {
        if (startXColumn > rectPosition.startXColumn) return true;
        return false;
    }

    public virtual AERectPosition Clone()
    {
        AERectPosition result = new AERectPosition(offsetStartXColumn, offsetStartY_, startXColumn, startY_, width_, widthOld_, height_, separator_, windowWidth_, tab_, tabCount);
        result.heightOne = heightOne;
        return result;
    }

    private int tab_ = 12;

    public int tab
    {
        get
        {
            return tab_;
        }

        set
        {
            tab_ = value;
        }
    }

    private int tabCount = 0;
    public void Tab()
    {
        tabCount++;
        offsetStartX_ += tab_;
    }

    public int tabs
    {
        get
        {
            return tab_ * tabCount;
        }
    }

    public void TabEnd()
    {
        tabCount--;
        offsetStartX_ -= tab_;
    }
}