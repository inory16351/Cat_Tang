# Lobby UI image generation

Mode: built-in image_gen tool. Reference: ref_hud.png.
Final PNG export: normalize magenta pixels to RGB(255,0,255); pack four icons into equal 256x256 cells without stretching.

## title

Use case: logo-brand. Create ONE final game UI raster asset, filename title_1159.png, exact landscape canvas 1536 x 1024. Input reference: supplied HUD image is STYLE REFERENCE ONLY (brass, gold, cream, deep burgundy, dark leather, cel shading, thick almost-black ink contours, restrained hatched wear). Do not reproduce the HUD.
BACKGROUND: perfectly flat, uniform solid RGB(255,0,255) #FF00FF over the entire empty canvas including holes. No gradient, no glow, no shadow cast onto the magenta. Opaque background, not transparency. Center the artwork with generous clean margin.
SUBJECT: GAME TITLE LOGO with typography reading EXACTLY "11:59": digit one, digit one, colon, digit five, digit nine, in that order. No other text. Extremely readable big chunky western wood-type / circus poster numerals made of polished brass and gold, dark ink outlines and deep burgundy attached drop-shadow extrusion. Numerals melt like a Dali clock: honey-gold drips descending from the bottoms of the digits, with the 9 slightly sagging while remaining clearly a nine. Colon consists of exactly two SMALL ROUND POCKET-WATCH FACES stacked vertically, recognizable as punctuation, each with cream dial and simple hands. Thin brass gear and clock-tick ornament line beneath the title. Wide horizontal composition filling about 85 percent of canvas width, all decoration kept inside margins. Bold cel-shaded hand-inked game art matching supplied style; clean readable silhouette, flat frontal view. No extra words, labels, watermark, mockup, borders or surrounding objects. The attached burgundy extrusion belongs to the logo and must have a hard ink edge; no atmospheric shadows on background.

## button

Use case: ui-mockup. Create ONE finished sprite sheet, filename lobby_button.png, exact canvas 1536 x 1024 landscape. Input reference HUD is STYLE REFERENCE ONLY: ornate brass hardware, gold, rivets, dark leather, bold almost-black hand-ink outlines, simple cel-shaded highlights, restrained hatching.
BACKGROUND: perfectly uniform opaque RGB(255,0,255) #FF00FF. No gradient, texture, glow or cast shadow on the magenta, including around the hover plate.
LAYOUT: exactly TWO identical wide horizontal BUTTON PLATES stacked vertically, aligned and centered, generous margins and clear empty gap. Each about 1200 x 300 pixels. Top center approx (768,300), bottom center approx (768,724). Both fully visible. Flat frontal view, no perspective.
TOP = normal state: riveted aged-brass frame around a very wide plain empty dark aged-leather center. Small gear rivets at both ends. Tiny clock-hand ornament integrated into LEFT end of brass frame, outside usable center. Dark brown leather must be plain, featureless, empty and suitable for adding text in-game; subtle leather material is okay. No decorative patterns within center.
BOTTOM = hover state: EXACT SAME plate silhouette, dimensions, ornaments and rivet positions as top, but brighter golden brass frame and restrained warm amber glow confined INSIDE the leather panel. Large center is still empty. Bold clean cel-shaded graphic game asset matching reference HUD. No text, letters, digits, labels, watermark, symbols in leather center, additional plates or objects. No outer luminous halo.

## card

Use case: ui-mockup. Create ONE finished game SAVE SLOT CARD frame, filename slot_card.png. Exact portrait canvas 1024 x 1536.
Reference HUD image is STYLE REFERENCE ONLY: aged brass and golden mechanical parts, rivets, cream watch faces, dark leather, bold almost-black ink contours, cel-shaded solid highlights, restrained hand-drawn hatching.
BACKGROUND: perfectly flat uniform solid RGB(255,0,255) #FF00FF everywhere outside the object. No gradient, no glow, no shadows on the magenta. Opaque chroma-key canvas.
SUBJECT: a tall save slot card shaped like the back of an opened antique pocket-watch case / clockmaker's memory locket, as a flat front-facing game UI frame. Centered with generous margin, all elements completely visible. Ornate brass rim with rivets, a few SMALL integrated gears, subtly rounded locket-like top and bottom, cream dial inset at the TOP CENTER. Dial is round and relatively small, leaving about 70 percent of card area below it as ONE LARGE PLAIN EMPTY DARK-LEATHER PANEL, clear uninterrupted rectangular-ish interior for in-game text. Keep ornament confined to the outer frame. No ornament or dividing lines in the dark leather panel. Small gold melting drip over ONE corner of brass frame.
WATCH: plain cream round dial. Absolutely no letters or written numerals; use twelve tiny simple radial strokes as Roman-numeral-like tick marks only. Hands indicate precisely 11:59: minute hand one minute counterclockwise from straight up, hour hand just before twelve, separated enough to see two thin hands near the top. Brass pivot.
No text, letters, words, labels, captions, digits, watermark or additional objects. Cel-shaded bold hand-inked game asset matching supplied HUD; avoid photorealism and excessive micro-detail. The large empty leather panel is the dominant functional area.

## card_revision

Use case: precise-object-edit. Revise this SAVE SLOT CARD image into a final 1024x1536 portrait game sprite. Preserve brass, rivets, ink contours, plain dark leather, cream dial, one melting drip, and cel-shaded HUD art style. Make the top watch MUCH SMALLER and remove the red fabric and big flanking gears. IMPORTANT LAYOUT: total complete card outer bounds x=120..904 and y=130..1406, including top loop. Top small round cream watch center at x512 y300, diameter 210 pixels; hands at 11:59, plain radial ticks, no letters or numerals. The large EMPTY plain dark brown leather panel extends x=175..849, y=430..1325. Thin ornate brass frame with small gear rivets and softly rounded pocket-watch-case corners. This makes leather about 70 percent of visible card body. All decorations remain around outer rim; no dividing lines or markings on leather. Small melting gold drip on bottom right corner. Clockmaker memory locket silhouette with minimal top hardware, no giant header.
Keep the object centered with GENEROUS MAGENTA MARGINS at all sides, at least 110 pixels. BACKGROUND must be PERFECTLY UNIFORM SOLID #FF00FF RGB(255,0,255), no noise, no gradient, no shadow, no glow. No text, letters or digits anywhere. Only produce the isolated single card.

## icons

Use case: ui-mockup. Create ONE finished game icon sprite strip lobby_icons.png, exact canvas 1024 x 256, aspect ratio FOUR TO ONE. Input HUD is style reference only: aged brass/gold, cream, wood, bold almost-black ink outline, simple CEL SHADING, restrained hatching, game UI readable at small size.
BACKGROUND: opaque perfectly flat solid magenta RGB(255,0,255) #FF00FF. No gradient, glow, texture or cast shadow on background. Background is continuous, no visible cell borders.
LAYOUT: exactly FOUR evenly spaced icons, one inside each equal 256x256 cell. Centers at (128,128), (384,128), (640,128), (896,128). Each isolated icon fits within 184x196 pixels with generous surrounding magenta, no overlap. Consistent visual weight, clean frontal view.
From LEFT TO RIGHT:
1. PLAY icon: a gold triangular right-pointing play arrow made from a broad ornate brass clock hand, circular pivot at left and unmistakable triangular point toward right.
2. NEW icon: a cream-faced brass pocket watch with simple unnumbered dial and hands, a small bright plus-shaped four-point spark attached near upper right.
3. DELETE icon: a cracked hourglass with brass top and bottom, fractured cream-tinted glass, golden sand spilling through the crack onto a tiny contained pile.
4. QUIT icon: a wooden WESTERN SALOON DOOR in a compact brass-trimmed wooden doorway, TWO swinging half doors visibly open, showing deep dark interior, readable open passage. Simple silhouette.
No text, NO WORDS including PLAY NEW DELETE QUIT, no letters, no numbers, no labels, no watermark, no additional icons or framing. Match reference's bold cel-shaded hand-drawn brass western clockwork style. All four assets fully visible.
