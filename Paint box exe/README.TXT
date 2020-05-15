Simple paint box. Paint analog based on winforms.
Further, i will shortly describe main functions of this app.

-File tab page (Also are accessible in the upper task toolstrip):

1.) Create button. Allows you to create new canvas to draw on
with specific size and background color.

2.) Open button. Opens any image and adapts your canvas for it. It also
adds it to the hierarchy. (DragNDrop is also avaible)

3.) Save button. Save image on your canvas in any format you need to the specific location.

4.) Print. Prints image.

* Undo (toolstrip) - cancels the last action.

* Redo (toolstrip) - cancels the last cancellation 

-Main tab page:

###################### Pen/Size section ######################

1.) Lines. You can change their color, size and style by using upper tab control.

* Size of the line can be changed by using numeric Up/Down element.

* When you choose color, the programm will automatically change your cursor to pen mode, so if
you for instance have been using formbuilder and then decided to change the color, programm will
change your cursor from form drawing to line drawing.

2.) Fast access color panel. Panel which will store colors and add new if it is neeeded,

* You can also remove color, which you don't need. First you need to choose it and then
click right mouse button and choose remove button.

3.) ColorChanger. It will allow you to choose color from bigger amount of variants, if 
the color is not already in the fast access panel, it will be added to it.

4.) Pipette. Instrument, which allows you to gain color of the specific location on the image and
then use it. This function also adds color to the fast access panel, if it isn't there.

If you change the size of the app, Pen/Size section will shorten itself to fit in.

###################### Style-Form section ######################

1.) Pen style. It is style of the pen, or the borders of the figures you can draw.

2.) Figure style. It can have draw or fill style.

3.) Forms. Allows you to draw line, rectangle, ellipse or put text in specific position.

*Text has it own form in which you will edit it and choose font and color.

----------------------------------------------------------------------------------------------------------------

What is hierarchy window?

It is used for storing all the objects you draw in the specific order.
The objects which have the large id number are the ones, which were painted last, and that
also means they are on the foreground. And of course the ones, which are close to the first number are on the
background.

By using hierarchy window, you change position of the element (Move it upper or lower in hierarchy or remove it).
You can do that by choosing the element, which will be highlighted, and pressing the right mouse button.
