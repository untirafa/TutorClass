import {test} from 'node:test';
import assert from 'node:assert/strict';
import {math,render} from '../TutorClass.API/wwwroot/js/lib/markdown.js';
class Node {
  constructor(tag){this.tagName=tag.toUpperCase();this.children=[];this.value='';}
  append(...children){this.children.push(...children);}
  replaceChildren(){this.children=[];this.value='';}
  set textContent(text){this.value=text;this.children=[];}
  get textContent(){return this.value+this.children.map(n=>n.textContent).join('');}
  set innerHTML(_){throw new Error('HTML não confiável');}
}
globalThis.document={createElement:tag=>new Node(tag),createTextNode:text=>({textContent:text})};
const renderer={math,render};
test('títulos, negrito e listas viram elementos; frações e acentos ficam legíveis',()=>{
  const target=new Node('div');renderer.render(target,'### Frações\n**Atenção:** \\(\\frac{1}{2}\\)\n- Ação');
  assert.equal(target.children[0].tagName,'H4');
  assert.equal(target.children[1].children[1].tagName,'STRONG');
  assert.equal(target.children[2].tagName,'UL');
  assert.equal(target.textContent,'FraçõesAtenção: (1)/(2)Ação');
});
test('HTML da resposta permanece texto e código não perde asteriscos',()=>{
  const target=new Node('div');renderer.render(target,'<img src=x onerror=alert(1)>\n```js\nconst x = a ** 2;\n```');
  assert.ok(target.textContent.includes('<img src=x onerror=alert(1)>'));
  assert.ok(target.textContent.includes('a ** 2'));
  assert.equal(target.children[1].tagName,'PRE');
});
test('frações aninhadas, raiz, potências e moeda preservam significado',()=>{
  assert.equal(renderer.math('\\frac{1}{\\frac{2}{3}}'),'(1)/((2)/(3))');
  assert.equal(renderer.math('\\sqrt{x} + x^{2} \\times \\pi'),'√(x) + x^(2) × π');
  assert.equal(renderer.math('R$ 10 e R$ 20'),'R$ 10 e R$ 20');
});

test('exemplos das capturas: questões, separador, dica e fórmula do vértice',()=>{
  const target=new Node('div');
  renderer.render(target,'**Questão 1 (Citologia)**\n---\n**Questão 2 (Ecologia)**\n$$t_v = \\frac{-b}{2a}$$\n*Dica: Na equação, $a = -5$ e $b = 30$.*');
  assert.equal(target.children[1].tagName,'HR');
  assert.equal(target.children[4].children[1].tagName,'EM');
  assert.equal(target.textContent,'Questão 1 (Citologia)Questão 2 (Ecologia)t_v = (-b)/(2a)Dica: Na equação, a = -5 e b = 30.');
});

test('asteriscos de multiplicação e código permanecem intactos',()=>{
  const target=new Node('div');renderer.render(target,'2 * 3 * 4\n`**texto** \\frac{1}{2}`');
  assert.equal(target.textContent,'2 * 3 * 4**texto** \\frac{1}{2}');
});

test('itálico envolvendo negritos no exemplo de velocidade média',()=>{
  const target=new Node('div');
  renderer.render(target,'&#x20;Vamos calcular a **velocidade média**.\n*Um carro percorre **150 quilômetros (km)** em **2 horas (h)**.*');
  assert.equal(target.textContent,' Vamos calcular a velocidade média.Um carro percorre 150 quilômetros (km) em 2 horas (h).');
  const emphasis=target.children[1].children[1];
  assert.equal(emphasis.tagName,'EM');
  assert.equal(emphasis.children.filter(n=>n.tagName==='STRONG').length,2);
});
