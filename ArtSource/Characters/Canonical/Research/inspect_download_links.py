from html.parser import HTMLParser
from pathlib import Path
import re
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for k,v in attrs:
            if v and any(x in v.lower() for x in ('.zip','eric','free-rigged','.js')):
                print(k,v.encode('ascii','replace').decode()[:600])
p=Path(__file__).with_name('renderpeople-free-page.html')
Links().feed(p.read_text(encoding='utf-8'))
