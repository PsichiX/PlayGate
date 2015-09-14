{
  function variantExists(v){
    for(var i = 0, c = v.length; i < c; ++i){
      if(variants.indexOf(v[i]) < 0) return false;
    }
    return true;
  }
}

start
  = op_and

ws
  = ws:[ \t]*

op_and
  = ws0:ws left:op_or "&" right:op_and ws1:ws { return left && right; }
  / op_or

op_or
  = ws0:ws left:op_not "|" right:op_or ws1:ws { return left || right; }
  / op_not

op_not
  = ws0:ws "!" value:primary ws1:ws { return !value; }
  / primary

primary
  = variant
  / ws0:ws "(" op_and:op_and ")" ws1:ws { return op_and; }

variant "variant"
  = ws0:ws name:[_a-zA-Z0-9]+ ws1:ws { return variantExists(name); }
